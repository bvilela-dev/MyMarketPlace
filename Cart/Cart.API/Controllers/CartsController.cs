using Cart.Application.Commands;
using Cart.Application.Queries;
using Cart.Domain.Entities;
using Marketplace.Infrastructure.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Controllers;

/// <summary>
/// Endpoints do carrinho de compras do usuario autenticado.
/// </summary>
/// <remarks>
/// <para>
/// <b>Correcao de seguranca.</b> Antes o carrinho era acessado por
/// <c>/api/carts/{userId}</c> sem qualquer autenticacao: bastava trocar o GUID para ler
/// ou sobrescrever o carrinho de qualquer pessoa. Agora nao existe mais <c>{userId}</c>
/// na rota — o dono e sempre quem esta no token.
/// </para>
/// <para>
/// Remover o parametro e melhor do que valida-lo: uma rota que nao aceita o id de outro
/// usuario nao tem como ser usada de forma errada.
/// </para>
/// </remarks>
/// <param name="sender">Mediator usado para despachar comandos e consultas.</param>
/// <param name="currentUser">Usuario autenticado na requisicao atual.</param>
[ApiController]
[Authorize]
[Route("api/carts")]
[Produces("application/json")]
public sealed class CartsController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Retorna o carrinho do usuario autenticado.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Carrinho atual.</returns>
    /// <response code="200">Carrinho encontrado.</response>
    /// <response code="401">Token ausente ou invalido.</response>
    /// <response code="404">O usuario ainda nao tem carrinho.</response>
    [HttpGet("me")]
    [ProducesResponseType<ShoppingCart>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShoppingCart>> GetCurrent(CancellationToken cancellationToken)
    {
        var cart = await sender.Send(new GetCartQuery(currentUser.RequireUserId()), cancellationToken);
        return cart is null ? NotFound() : Ok(cart);
    }

    /// <summary>
    /// Cria ou substitui o carrinho do usuario autenticado.
    /// </summary>
    /// <remarks>
    /// <c>PUT</c> porque a operacao substitui o recurso inteiro e e idempotente: enviar
    /// o mesmo corpo varias vezes leva sempre ao mesmo estado final.
    /// </remarks>
    /// <param name="request">Conteudo completo do carrinho.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Carrinho gravado, ja consolidado.</returns>
    /// <response code="200">Carrinho gravado.</response>
    /// <response code="400">Itens invalidos.</response>
    /// <response code="401">Token ausente ou invalido.</response>
    /// <response code="409">Quantidade de produtos distintos acima do limite.</response>
    [HttpPut("me")]
    [ProducesResponseType<ShoppingCart>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ShoppingCart> Put([FromBody] UpsertCartRequest request, CancellationToken cancellationToken)
    {
        var items = request.Items
            .Select(item => new CartItem(item.ProductId, item.Name, item.UnitPrice, item.Quantity))
            .ToArray();

        // Antes, este metodo devolvia Task<object> e usava ContinueWith para converter
        // o resultado. Alem de ilegivel, ContinueWith empacota qualquer falha numa
        // AggregateException — o que fazia o middleware de erros perder o tipo original
        // da excecao e devolver 500 no lugar do status correto.
        return sender.Send(new UpsertCartCommand(currentUser.RequireUserId(), items), cancellationToken);
    }

    /// <summary>
    /// Esvazia o carrinho do usuario autenticado.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Resposta sem conteudo.</returns>
    /// <response code="204">Carrinho removido (ou ja inexistente).</response>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        await sender.Send(new ClearCartCommand(currentUser.RequireUserId()), cancellationToken);

        // 204 mesmo quando o carrinho nao existia: DELETE e idempotente por definicao,
        // e o estado desejado ("nao ha carrinho") foi alcancado de qualquer forma.
        return NoContent();
    }
}

/// <summary>
/// Corpo da requisicao de gravacao do carrinho.
/// </summary>
/// <param name="Items">Conteudo completo do carrinho.</param>
public sealed record UpsertCartRequest(IReadOnlyCollection<UpsertCartItemRequest> Items);

/// <summary>
/// Linha do carrinho no corpo da requisicao.
/// </summary>
/// <param name="ProductId">Produto.</param>
/// <param name="Name">Nome exibido do produto.</param>
/// <param name="UnitPrice">Preco unitario exibido.</param>
/// <param name="Quantity">Quantidade desejada.</param>
public sealed record UpsertCartItemRequest(Guid ProductId, string Name, decimal UnitPrice, int Quantity);
