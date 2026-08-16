using MediatR;

namespace Order.Application.Orders;

/// <summary>
/// Comando de criacao de pedido.
/// </summary>
/// <remarks>
/// <b>Correcao de seguranca:</b> o <paramref name="UserId"/> e preenchido pelo
/// controller a partir da claim <c>sub</c> do token — nunca pelo corpo da requisicao.
/// Antes, o cliente enviava o comando inteiro e podia criar pedidos em nome de qualquer
/// usuario, bastando trocar o GUID no JSON.
/// </remarks>
/// <param name="UserId">Usuario autenticado (vem do token).</param>
/// <param name="AddressId">Endereco de entrega escolhido, pertencente ao usuario.</param>
/// <param name="Items">Itens solicitados.</param>
public sealed record CreateOrderCommand(Guid UserId, Guid AddressId, IReadOnlyCollection<CreateOrderItemRequest> Items)
    : IRequest<CreateOrderResponse>;

/// <summary>
/// Linha solicitada num pedido.
/// </summary>
/// <remarks>
/// Repare que <b>nao existe campo de preco</b>. O preco vem do Catalog, no servidor.
/// Aceita-lo do cliente permitiria comprar qualquer produto por um centavo.
/// </remarks>
/// <param name="ProductId">Produto desejado.</param>
/// <param name="Quantity">Quantidade desejada.</param>
public sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);

/// <summary>
/// Resposta da criacao de pedido.
/// </summary>
/// <param name="OrderId">Identificador do pedido criado.</param>
/// <param name="UserId">Usuario dono do pedido.</param>
/// <param name="Total">Valor total calculado pelo servidor.</param>
/// <param name="Currency">Moeda do total.</param>
/// <param name="Status">
/// Estado inicial, sempre <c>PendingPayment</c>. O status evolui de forma assincrona —
/// consulte <c>GET /api/orders/{id}</c> para acompanhar.
/// </param>
/// <param name="CreatedAtUtc">Momento (UTC) da criacao.</param>
public sealed record CreateOrderResponse(Guid OrderId, Guid UserId, decimal Total, string Currency, string Status, DateTime CreatedAtUtc);
