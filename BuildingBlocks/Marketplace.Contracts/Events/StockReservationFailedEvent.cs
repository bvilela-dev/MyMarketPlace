namespace Marketplace.Contracts.Events;

/// <summary>
/// Nao foi possivel reservar o estoque de um pedido ja pago.
/// </summary>
/// <remarks>
/// <para>
/// Este e o evento mais interessante da coreografia, porque expoe o problema central de
/// um saga: <b>o dinheiro ja foi cobrado e a mercadoria nao existe.</b>
/// </para>
/// <para>
/// Nao ha rollback distribuido — cada servico ja commitou a sua transacao local. O que
/// existe e a <b>transacao compensatoria</b>: uma nova operacao de negocio que desfaz o
/// efeito da anterior. Aqui o <b>Order</b> cancela o pedido e o <b>Notification</b>
/// avisa o cliente.
/// </para>
/// <para>
/// <b>Escopo do projeto:</b> o estorno automatico no Payment nao foi implementado — o
/// pedido apenas entra em <c>Cancelled</c>, marcando o ponto exato onde entraria a
/// integracao com o adquirente. Modelar o evento e o status ja deixa claro que o
/// cenario foi considerado, em vez de ignorado.
/// </para>
/// </remarks>
/// <param name="EventId">Identificador unico desta ocorrencia.</param>
/// <param name="OrderId">Pedido que nao pode ser atendido.</param>
/// <param name="UserId">Usuario dono do pedido.</param>
/// <param name="Reason">Motivo legivel da falha (ex.: produto sem saldo).</param>
/// <param name="FailedAtUtc">Momento (UTC) da falha.</param>
public sealed record StockReservationFailedEvent(Guid EventId, Guid OrderId, Guid UserId, string Reason, DateTime FailedAtUtc);
