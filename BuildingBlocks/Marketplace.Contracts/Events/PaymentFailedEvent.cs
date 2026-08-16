namespace Marketplace.Contracts.Events;

/// <summary>
/// A tentativa de pagamento de um pedido falhou.
/// </summary>
/// <remarks>
/// Publicado pelo <b>Payment</b>; consumido pelo <b>Order</b> (que move o pedido para
/// <c>PaymentFailed</c>) e pelo <b>Notification</b> (que avisa o cliente).
/// <para>
/// Este e o caminho de compensacao mais simples da coreografia: como nada havia sido
/// reservado ainda, basta marcar o pedido. Ja uma falha depois da reserva de estoque
/// exigiria uma <i>transacao compensatoria</i> devolvendo as unidades — ver
/// <see cref="StockReservationFailedEvent"/>.
/// </para>
/// </remarks>
/// <param name="EventId">Identificador unico desta ocorrencia.</param>
/// <param name="OrderId">Pedido afetado.</param>
/// <param name="UserId">Usuario dono do pedido.</param>
/// <param name="Reason">Motivo legivel da recusa.</param>
/// <param name="FailedAtUtc">Momento (UTC) da falha.</param>
public sealed record PaymentFailedEvent(Guid EventId, Guid OrderId, Guid UserId, string Reason, DateTime FailedAtUtc);
