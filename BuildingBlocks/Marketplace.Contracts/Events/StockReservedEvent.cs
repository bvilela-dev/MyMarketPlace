namespace Marketplace.Contracts.Events;

/// <summary>
/// O estoque de um pedido foi reservado com sucesso.
/// </summary>
/// <remarks>
/// Publicado pelo <b>Inventory</b> apos baixar as quantidades. E o evento que fecha o
/// fluxo feliz: o <b>Order</b> move o pedido para <c>Confirmed</c> e o
/// <b>Notification</b> avisa que a separacao comecou.
/// </remarks>
/// <param name="EventId">Identificador unico desta ocorrencia.</param>
/// <param name="OrderId">Pedido cujo estoque foi reservado.</param>
/// <param name="UserId">Usuario dono do pedido.</param>
/// <param name="ReservedAtUtc">Momento (UTC) da reserva.</param>
public sealed record StockReservedEvent(Guid EventId, Guid OrderId, Guid UserId, DateTime ReservedAtUtc);
