using Marketplace.Infrastructure.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.Application.Orders;

namespace Order.API.Controllers;

/// <summary>
/// Endpoints de criacao e consulta de pedidos.
/// </summary>
/// <remarks>
/// Todos exigem autenticacao: pedido e sempre de alguem. O identificador do usuario vem
/// do token, nunca da URL ou do corpo — ver <see cref="ICurrentUser"/>.
/// </remarks>
/// <param name="sender">Mediator usado para despachar comandos e consultas.</param>
/// <param name="currentUser">Usuario autenticado na requisicao atual.</param>
[ApiController]
[Authorize]
[Route("api/orders")]
[Produces("application/json")]
public sealed class OrdersController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Cria um pedido para o usuario autenticado.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A resposta chega com status <c>PendingPayment</c>. O restante do fluxo e
    /// <b>assincrono</b>: o pedido segue por eventos ate <c>Confirmed</c> (ou
    /// <c>PaymentFailed</c>/<c>Cancelled</c>). Consulte
    /// <c>GET /api/orders/{id}</c> para acompanhar a evolucao.
    /// </para>
    /// <para>
    /// <b>Correcao de seguranca:</b> antes, o <c>userId</c> vinha no corpo da requisicao
    /// e qualquer cliente podia criar pedidos em nome de terceiros. Agora ele e
    /// preenchido a partir da claim <c>sub</c> do token.
    /// </para>
    /// </remarks>
    /// <param name="request">Endereco de entrega e itens desejados.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Pedido criado.</returns>
    /// <response code="201">Pedido criado e aguardando pagamento.</response>
    /// <response code="400">Payload invalido.</response>
    /// <response code="401">Token ausente ou invalido.</response>
    /// <response code="409">Endereco invalido, produto sem estoque ou servico dependente fora do ar.</response>
    [HttpPost]
    [ProducesResponseType<CreateOrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateOrderResponse>> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(currentUser.RequireUserId(), request.AddressId, request.Items);
        var response = await sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = response.OrderId }, response);
    }

    /// <summary>
    /// Consulta um pedido do usuario autenticado.
    /// </summary>
    /// <remarks>
    /// E este endpoint que torna a coreografia observavel: chamando-o algumas vezes
    /// apos criar o pedido, o status caminha de <c>PendingPayment</c> para <c>Paid</c> e
    /// depois <c>Confirmed</c>, conforme os eventos sao processados.
    /// </remarks>
    /// <param name="id">Identificador do pedido.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Dados completos do pedido.</returns>
    /// <response code="200">Pedido encontrado.</response>
    /// <response code="404">Pedido inexistente ou pertencente a outro usuario.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await sender.Send(new GetOrderByIdQuery(id, currentUser.RequireUserId()), cancellationToken);

        // 404 (e nao 403) quando o pedido e de outro usuario: responder "existe, mas nao
        // e seu" confirmaria a existencia do recurso alheio. Do ponto de vista deste
        // usuario, o pedido simplesmente nao existe.
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>
    /// Lista os pedidos do usuario autenticado.
    /// </summary>
    /// <param name="page">Numero da pagina (base 1).</param>
    /// <param name="pageSize">Itens por pagina (maximo 100).</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Pedidos do usuario, do mais recente para o mais antigo.</returns>
    /// <response code="200">Lista retornada.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<OrderSummaryDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyCollection<OrderSummaryDto>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => sender.Send(new ListUserOrdersQuery(currentUser.RequireUserId(), page, pageSize), cancellationToken);
}

/// <summary>
/// Corpo da requisicao de criacao de pedido.
/// </summary>
/// <remarks>
/// Separado de <c>CreateOrderCommand</c> exatamente porque <b>nao</b> tem
/// <c>UserId</c>: o que o cliente nao pode influenciar, o cliente nao envia.
/// </remarks>
/// <param name="AddressId">Endereco de entrega, obtido em <c>GET /api/users/me</c>.</param>
/// <param name="Items">Itens desejados (produto e quantidade).</param>
public sealed record CreateOrderRequest(Guid AddressId, IReadOnlyCollection<CreateOrderItemRequest> Items);
