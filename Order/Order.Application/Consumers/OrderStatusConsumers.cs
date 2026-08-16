using Marketplace.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Application.Abstractions;

namespace Order.Application.Consumers;

/// <summary>
/// Base dos consumidores que apenas fazem o pedido avancar de estado.
/// </summary>
/// <remarks>
/// <para>
/// Os quatro consumidores do Order seguem exatamente o mesmo roteiro: carregar o pedido,
/// tentar a transicao, salvar. Extrair esse roteiro para uma classe base evita repetir
/// quatro vezes o mesmo tratamento de "pedido inexistente" e "transicao ignorada".
/// </para>
/// <para>
/// <b>Duas situacoes tratadas como sucesso — e nao como erro:</b>
/// </para>
/// <list type="number">
///   <item><b>Pedido nao encontrado.</b> Pode ser um evento de um ambiente antigo ou de
///   um pedido ja removido. Lancar excecao faria o MassTransit tentar de novo tres vezes
///   e depois mandar a mensagem para a fila de erro — barulho para algo que jamais vai
///   funcionar numa retentativa.</item>
///   <item><b>Transicao ignorada.</b> Significa que o pedido ja estava no estado
///   esperado (mensagem duplicada) ou num estado incompativel (chegada fora de ordem).
///   Nos dois casos o resultado desejado ja esta garantido.</item>
/// </list>
/// <para>
/// A regra geral: em consumidor de fila, so lance excecao quando <b>tentar de novo pode
/// resolver</b>. Erro permanente que vira retry vira alarme falso.
/// </para>
/// </remarks>
/// <typeparam name="TEvent">Tipo do evento de integracao consumido.</typeparam>
/// <param name="dbContext">Contrato de persistencia do Order.</param>
/// <param name="logger">Logger do consumidor.</param>
public abstract class OrderStatusConsumerBase<TEvent>(IOrderDbContext dbContext, ILogger logger) : IConsumer<TEvent>
    where TEvent : class
{
    /// <summary>
    /// Extrai do evento o identificador do pedido afetado.
    /// </summary>
    /// <param name="message">Evento recebido.</param>
    /// <returns>Identificador do pedido.</returns>
    protected abstract Guid GetOrderId(TEvent message);

    /// <summary>
    /// Aplica a transicao de estado correspondente ao evento.
    /// </summary>
    /// <param name="order">Pedido carregado do banco.</param>
    /// <param name="message">Evento recebido.</param>
    /// <param name="utcNow">Instante atual em UTC.</param>
    /// <returns><see langword="true"/> quando o estado mudou.</returns>
    protected abstract bool ApplyTransition(Domain.Entities.Order order, TEvent message, DateTime utcNow);

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<TEvent> context)
    {
        var orderId = GetOrderId(context.Message);

        var order = await dbContext.Orders
            .FirstOrDefaultAsync(candidate => candidate.Id == orderId, context.CancellationToken);

        if (order is null)
        {
            logger.LogWarning(
                "Evento {EventType} ignorado: pedido {OrderId} nao encontrado.",
                typeof(TEvent).Name,
                orderId);
            return;
        }

        var previousStatus = order.Status;

        if (!ApplyTransition(order, context.Message, DateTime.UtcNow))
        {
            logger.LogInformation(
                "Evento {EventType} ignorado para o pedido {OrderId}: estado atual {Status} nao permite a transicao (duplicata ou fora de ordem).",
                typeof(TEvent).Name,
                orderId,
                previousStatus);
            return;
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "Pedido {OrderId}: {PreviousStatus} -> {NewStatus}.",
            orderId,
            previousStatus,
            order.Status);
    }
}

/// <summary>
/// Move o pedido de <c>PendingPayment</c> para <c>Paid</c> quando o pagamento e aprovado.
/// </summary>
/// <param name="dbContext">Contrato de persistencia do Order.</param>
/// <param name="logger">Logger do consumidor.</param>
public sealed class PaymentApprovedConsumer(IOrderDbContext dbContext, ILogger<PaymentApprovedConsumer> logger)
    : OrderStatusConsumerBase<PaymentApprovedEvent>(dbContext, logger)
{
    /// <inheritdoc />
    protected override Guid GetOrderId(PaymentApprovedEvent message) => message.OrderId;

    /// <inheritdoc />
    protected override bool ApplyTransition(Domain.Entities.Order order, PaymentApprovedEvent message, DateTime utcNow)
        => order.MarkAsPaid(utcNow);
}

/// <summary>
/// Move o pedido para <c>PaymentFailed</c> quando o pagamento e recusado.
/// </summary>
/// <param name="dbContext">Contrato de persistencia do Order.</param>
/// <param name="logger">Logger do consumidor.</param>
public sealed class PaymentFailedConsumer(IOrderDbContext dbContext, ILogger<PaymentFailedConsumer> logger)
    : OrderStatusConsumerBase<PaymentFailedEvent>(dbContext, logger)
{
    /// <inheritdoc />
    protected override Guid GetOrderId(PaymentFailedEvent message) => message.OrderId;

    /// <inheritdoc />
    protected override bool ApplyTransition(Domain.Entities.Order order, PaymentFailedEvent message, DateTime utcNow)
        => order.MarkPaymentAsFailed(message.Reason, utcNow);
}

/// <summary>
/// Move o pedido de <c>Paid</c> para <c>Confirmed</c> quando o estoque e reservado.
/// </summary>
/// <param name="dbContext">Contrato de persistencia do Order.</param>
/// <param name="logger">Logger do consumidor.</param>
public sealed class StockReservedConsumer(IOrderDbContext dbContext, ILogger<StockReservedConsumer> logger)
    : OrderStatusConsumerBase<StockReservedEvent>(dbContext, logger)
{
    /// <inheritdoc />
    protected override Guid GetOrderId(StockReservedEvent message) => message.OrderId;

    /// <inheritdoc />
    protected override bool ApplyTransition(Domain.Entities.Order order, StockReservedEvent message, DateTime utcNow)
        => order.Confirm(utcNow);
}

/// <summary>
/// Cancela o pedido quando o estoque nao pode ser reservado apos o pagamento.
/// </summary>
/// <remarks>
/// Este e o passo de <b>compensacao</b> do saga. O pedido entra em <c>Cancelled</c> com
/// o motivo registrado; num sistema completo, e daqui que sairia o comando de estorno
/// para o servico de pagamento.
/// </remarks>
/// <param name="dbContext">Contrato de persistencia do Order.</param>
/// <param name="logger">Logger do consumidor.</param>
public sealed class StockReservationFailedConsumer(IOrderDbContext dbContext, ILogger<StockReservationFailedConsumer> logger)
    : OrderStatusConsumerBase<StockReservationFailedEvent>(dbContext, logger)
{
    /// <inheritdoc />
    protected override Guid GetOrderId(StockReservationFailedEvent message) => message.OrderId;

    /// <inheritdoc />
    protected override bool ApplyTransition(Domain.Entities.Order order, StockReservationFailedEvent message, DateTime utcNow)
        => order.Cancel(message.Reason, utcNow);
}
