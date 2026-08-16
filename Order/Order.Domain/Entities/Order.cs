using Marketplace.SharedKernel.Abstractions;
using Marketplace.SharedKernel.Exceptions;
using Order.Domain.ValueObjects;

namespace Order.Domain.Entities;

/// <summary>
/// Agregado que representa um pedido.
/// </summary>
/// <remarks>
/// <para>
/// Raiz do agregado formado pelos <see cref="OrderItem"/> e pelo objeto de valor
/// <see cref="AddressSnapshot"/>. Concentra as duas invariantes do pedido:
/// </para>
/// <list type="number">
///   <item>o <see cref="Total"/> e sempre a soma dos itens — calculado no construtor,
///   nunca recebido de fora, para que ninguem consiga gravar um total divergente;</item>
///   <item>o <see cref="Status"/> so muda pelas transicoes validas da maquina de
///   estados (ver <see cref="OrderStatus"/>).</item>
/// </list>
/// </remarks>
public sealed class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = [];

    /// <summary>
    /// Construtor exigido pelo EF Core.
    /// </summary>
    private Order()
    {
    }

    /// <summary>
    /// Cria um pedido no estado <see cref="OrderStatus.PendingPayment"/>.
    /// </summary>
    /// <param name="id">Identificador do pedido.</param>
    /// <param name="userId">Usuario que fez o pedido.</param>
    /// <param name="addressSnapshot">Copia do endereco de entrega no momento da compra.</param>
    /// <param name="items">Itens do pedido (pelo menos um).</param>
    /// <param name="utcNow">
    /// Instante da criacao. Opcional: em producao fica <see langword="null"/> e o proprio
    /// agregado usa o relogio do sistema.
    /// <para>
    /// <b>Por que permitir injetar o tempo?</b> Porque <c>DateTime.UtcNow</c> cravado
    /// dentro da entidade torna o comportamento nao deterministico e impossivel de
    /// testar: nao ha como verificar "o carimbo foi atualizado" sem controlar o relogio.
    /// O mesmo motivo pelo qual todas as transicoes de estado recebem <c>utcNow</c>.
    /// </para>
    /// </param>
    /// <exception cref="BusinessRuleException">Lancada quando o pedido nao tem itens.</exception>
    public Order(
        Guid id,
        Guid userId,
        AddressSnapshot addressSnapshot,
        IReadOnlyCollection<OrderItem> items,
        DateTime? utcNow = null)
    {
        if (items.Count == 0)
        {
            throw new BusinessRuleException("O pedido precisa ter ao menos um item.");
        }

        Id = id;
        UserId = userId;
        AddressSnapshot = addressSnapshot;
        CreatedAtUtc = utcNow ?? DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        Status = OrderStatus.PendingPayment;

        _items.AddRange(items);

        // O total e derivado, jamais informado pelo cliente. Aceitar um total vindo de
        // fora permitiria a um cliente malicioso enviar itens caros com total de R$ 1.
        Total = _items.Sum(item => item.UnitPrice * item.Quantity);
    }

    /// <summary>
    /// Usuario dono do pedido.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Valor total, calculado a partir dos itens.
    /// </summary>
    public decimal Total { get; private set; }

    /// <summary>
    /// Estado atual do pedido.
    /// </summary>
    public OrderStatus Status { get; private set; } = OrderStatus.PendingPayment;

    /// <summary>
    /// Motivo do cancelamento ou da recusa, quando aplicavel.
    /// </summary>
    public string? StatusReason { get; private set; }

    /// <summary>
    /// Momento (UTC) da criacao.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Momento (UTC) da ultima mudanca de estado.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Endereco de entrega congelado no momento da compra.
    /// </summary>
    public AddressSnapshot AddressSnapshot { get; private set; } = null!;

    /// <summary>
    /// Itens do pedido.
    /// </summary>
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Marca o pedido como pago.
    /// </summary>
    /// <remarks>
    /// Transicao valida: <see cref="OrderStatus.PendingPayment"/> →
    /// <see cref="OrderStatus.Paid"/>.
    /// </remarks>
    /// <param name="utcNow">Instante atual em UTC.</param>
    /// <returns>
    /// <see langword="true"/> quando a transicao ocorreu; <see langword="false"/> quando
    /// foi ignorada por o pedido ja estar em outro estado.
    /// </returns>
    public bool MarkAsPaid(DateTime utcNow) => TryTransition(OrderStatus.Paid, OrderStatus.PendingPayment, utcNow);

    /// <summary>
    /// Registra a recusa do pagamento.
    /// </summary>
    /// <param name="reason">Motivo informado pelo servico de pagamento.</param>
    /// <param name="utcNow">Instante atual em UTC.</param>
    /// <returns><see langword="true"/> quando a transicao ocorreu.</returns>
    public bool MarkPaymentAsFailed(string reason, DateTime utcNow)
        => TryTransition(OrderStatus.PaymentFailed, OrderStatus.PendingPayment, utcNow, reason);

    /// <summary>
    /// Confirma o pedido apos a reserva de estoque.
    /// </summary>
    /// <param name="utcNow">Instante atual em UTC.</param>
    /// <returns><see langword="true"/> quando a transicao ocorreu.</returns>
    public bool Confirm(DateTime utcNow) => TryTransition(OrderStatus.Confirmed, OrderStatus.Paid, utcNow);

    /// <summary>
    /// Cancela um pedido ja pago que nao pode ser atendido.
    /// </summary>
    /// <param name="reason">Motivo do cancelamento.</param>
    /// <param name="utcNow">Instante atual em UTC.</param>
    /// <returns><see langword="true"/> quando a transicao ocorreu.</returns>
    public bool Cancel(string reason, DateTime utcNow) => TryTransition(OrderStatus.Cancelled, OrderStatus.Paid, utcNow, reason);

    /// <summary>
    /// Aplica uma transicao de estado quando o estado atual e o esperado.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Este metodo devolve <c>bool</c> em vez de lancar excecao — e essa e a decisao
    /// de design mais importante da classe.</b>
    /// </para>
    /// <para>
    /// A entrega de mensagens e <i>at-least-once</i>: o mesmo <c>PaymentApproved</c>
    /// pode chegar duas vezes (reentrega do RabbitMQ, republicacao do outbox, replay
    /// manual). Se a segunda chegada lancasse excecao, o MassTransit trataria como
    /// falha, tentaria de novo tres vezes e por fim jogaria a mensagem na fila de erro —
    /// gerando alarme para uma situacao <b>perfeitamente normal</b>.
    /// </para>
    /// <para>
    /// Retornando <see langword="false"/>, o consumidor apenas registra "ja processado"
    /// e conclui com sucesso. A operacao passa a ser <b>idempotente</b>: aplicar duas
    /// vezes tem o mesmo efeito de aplicar uma.
    /// </para>
    /// <para>
    /// Repare que isso tambem cobre chegada fora de ordem: se <c>StockReserved</c>
    /// chegasse antes de <c>PaymentApproved</c>, o pedido ainda estaria em
    /// <c>PendingPayment</c> e a confirmacao seria simplesmente ignorada, em vez de
    /// corromper o estado.
    /// </para>
    /// </remarks>
    /// <param name="target">Estado de destino.</param>
    /// <param name="requiredCurrent">Estado atual exigido para a transicao.</param>
    /// <param name="utcNow">Instante atual em UTC.</param>
    /// <param name="reason">Motivo associado a mudanca, quando houver.</param>
    /// <returns><see langword="true"/> quando o estado mudou.</returns>
    private bool TryTransition(OrderStatus target, OrderStatus requiredCurrent, DateTime utcNow, string? reason = null)
    {
        if (Status != requiredCurrent)
        {
            return false;
        }

        Status = target;
        StatusReason = reason;
        UpdatedAtUtc = utcNow;

        return true;
    }
}
