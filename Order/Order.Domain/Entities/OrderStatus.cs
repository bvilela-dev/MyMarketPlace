namespace Order.Domain.Entities;

/// <summary>
/// Estados possiveis de um pedido ao longo da coreografia de checkout.
/// </summary>
/// <remarks>
/// <para>
/// A maquina de estados completa:
/// </para>
/// <code>
///                       PaymentApproved            StockReserved
///  PendingPayment ────────────────────► Paid ──────────────────────► Confirmed
///         │                              │
///         │ PaymentFailed                │ StockReservationFailed
///         ▼                              ▼
///   PaymentFailed                    Cancelled
/// </code>
/// <para>
/// <b>Por que um enum e nao string solta?</b> Na versao anterior o status era
/// <c>string</c> com uma unica constante <c>"PendingPayment"</c> — e nada impedia
/// alguem de gravar <c>"pendente"</c>, <c>"PENDING"</c> ou um erro de digitacao. Com
/// enum, o compilador garante o conjunto fechado de valores e o <c>switch</c> exaustivo
/// avisa quando um estado novo nao foi tratado em algum ponto.
/// </para>
/// </remarks>
public enum OrderStatus
{
    /// <summary>
    /// Pedido criado, aguardando o processamento do pagamento.
    /// </summary>
    PendingPayment = 0,

    /// <summary>
    /// Pagamento aprovado; aguardando a reserva de estoque.
    /// </summary>
    Paid = 1,

    /// <summary>
    /// Estoque reservado; pedido confirmado e pronto para separacao.
    /// </summary>
    Confirmed = 2,

    /// <summary>
    /// Pagamento recusado. Nada foi reservado, entao nao ha o que compensar.
    /// </summary>
    PaymentFailed = 3,

    /// <summary>
    /// Pedido cancelado depois do pagamento — tipicamente por falta de estoque.
    /// </summary>
    /// <remarks>
    /// E o estado que sinaliza necessidade de <b>transacao compensatoria</b>: o valor ja
    /// foi cobrado e precisa ser estornado.
    /// </remarks>
    Cancelled = 4
}
