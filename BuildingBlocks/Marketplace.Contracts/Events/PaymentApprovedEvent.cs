namespace Marketplace.Contracts.Events;

/// <summary>
/// O pagamento de um pedido foi aprovado.
/// </summary>
/// <remarks>
/// <para>
/// Publicado pelo <b>Payment</b> e consumido por tres servicos ao mesmo tempo:
/// </para>
/// <list type="bullet">
///   <item><b>Inventory</b> — reserva o estoque dos itens;</item>
///   <item><b>Order</b> — muda o status do pedido para <c>Paid</c>;</item>
///   <item><b>Notification</b> — dispara o e-mail de confirmacao de pagamento.</item>
/// </list>
/// <para>
/// Repare que o Payment nao sabe que esses tres existem: apenas anuncia o fato. Adicionar
/// um quarto interessado (antifraude, BI) nao exige alterar uma linha do Payment. E o
/// beneficio central da <b>coreografia</b> sobre a <b>orquestracao</b>.
/// </para>
/// <para>
/// <b>O campo <see cref="Items"/> foi adicionado deliberadamente.</b> Sem ele, o
/// Inventory recebia apenas o total e nao tinha como saber <i>qual</i> produto baixar —
/// na versao anterior do projeto ele acabava decrementando um item arbitrario do
/// estoque, o que e um bug de negocio grave disfarcado de codigo funcional.
/// </para>
/// </remarks>
/// <param name="EventId">Identificador unico desta ocorrencia.</param>
/// <param name="OrderId">Pedido cujo pagamento foi aprovado.</param>
/// <param name="UserId">Usuario dono do pedido.</param>
/// <param name="Total">Valor aprovado.</param>
/// <param name="Items">Itens do pedido, necessarios para a reserva de estoque.</param>
/// <param name="ApprovedAtUtc">Momento (UTC) da aprovacao.</param>
public sealed record PaymentApprovedEvent(
    Guid EventId,
    Guid OrderId,
    Guid UserId,
    decimal Total,
    IReadOnlyCollection<OrderItemDto> Items,
    DateTime ApprovedAtUtc);
