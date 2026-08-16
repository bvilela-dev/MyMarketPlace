using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Messaging.Idempotency;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Consumers;

/// <summary>
/// Base dos consumidores de notificacao.
/// </summary>
/// <remarks>
/// <para>
/// Todos os consumidores deste servico seguem o mesmo roteiro: deduplicar e registrar a
/// notificacao que seria disparada. A classe base concentra a deduplicacao para que cada
/// consumidor concreto declare apenas <i>o que</i> notificar.
/// </para>
/// <para>
/// <b>Por que so log e nao envio real de e-mail?</b> Integrar com SendGrid/SES exigiria
/// credencial e traria pouca informacao nova sobre arquitetura. O que este servico
/// demonstra e o padrao: um consumidor por tipo de evento, cada um com sua fila propria
/// e idempotente. Trocar o <c>LogInformation</c> por uma chamada ao provedor de e-mail
/// e a parte trivial.
/// </para>
/// <para>
/// <b>Detalhe importante:</b> a deduplicacao usa o nome <b>qualificado</b> do consumidor
/// na chave. O mesmo <c>PaymentApprovedEvent</c> chega aqui e tambem no Inventory, e as
/// duas classes se chamam <c>PaymentApprovedConsumer</c>. Com o nome curto, os dois
/// compartilhariam a mesma chave e o segundo a processar descartaria a mensagem como
/// duplicada — bug que existiu de fato no projeto.
/// </para>
/// </remarks>
/// <typeparam name="TEvent">Tipo do evento consumido.</typeparam>
/// <param name="deduplicator">Deduplicador de mensagens.</param>
/// <param name="logger">Logger do consumidor.</param>
public abstract class NotificationConsumerBase<TEvent>(RedisMessageDeduplicator deduplicator, ILogger logger) : IConsumer<TEvent>
    where TEvent : class
{
    /// <summary>
    /// Logger disponivel para as classes derivadas.
    /// </summary>
    protected ILogger Logger { get; } = logger;

    /// <summary>
    /// Identificador do evento, usado na deduplicacao.
    /// </summary>
    /// <param name="message">Evento recebido.</param>
    /// <returns>Identificador da ocorrencia.</returns>
    protected abstract Guid GetEventId(TEvent message);

    /// <summary>
    /// Dispara a notificacao correspondente ao evento.
    /// </summary>
    /// <param name="message">Evento recebido.</param>
    /// <returns>Task da operacao assincrona.</returns>
    protected abstract Task NotifyAsync(TEvent message);

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<TEvent> context)
    {
        var messageId = context.MessageId ?? GetEventId(context.Message);

        // FullName (e nao Name): Inventory e Notification tem classes homonimas, e o
        // nome curto faria os dois compartilharem a mesma chave de deduplicacao.
        if (!await deduplicator.TryBeginAsync(messageId, GetType().FullName!, context.CancellationToken))
        {
            return;
        }

        await NotifyAsync(context.Message);
    }
}

/// <summary>
/// Envia o e-mail de boas-vindas quando um usuario e cadastrado.
/// </summary>
/// <param name="deduplicator">Deduplicador de mensagens.</param>
/// <param name="logger">Logger do consumidor.</param>
public sealed class UserCreatedConsumer(RedisMessageDeduplicator deduplicator, ILogger<UserCreatedConsumer> logger)
    : NotificationConsumerBase<UserCreatedEvent>(deduplicator, logger)
{
    /// <inheritdoc />
    protected override Guid GetEventId(UserCreatedEvent message) => message.EventId;

    /// <inheritdoc />
    protected override Task NotifyAsync(UserCreatedEvent message)
    {
        Logger.LogInformation("[E-MAIL] Boas-vindas para {Email} (usuario {UserId}).", message.Email, message.UserId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Avisa o cliente de que o pagamento foi aprovado.
/// </summary>
/// <param name="deduplicator">Deduplicador de mensagens.</param>
/// <param name="logger">Logger do consumidor.</param>
public sealed class PaymentApprovedConsumer(RedisMessageDeduplicator deduplicator, ILogger<PaymentApprovedConsumer> logger)
    : NotificationConsumerBase<PaymentApprovedEvent>(deduplicator, logger)
{
    /// <inheritdoc />
    protected override Guid GetEventId(PaymentApprovedEvent message) => message.EventId;

    /// <inheritdoc />
    protected override Task NotifyAsync(PaymentApprovedEvent message)
    {
        Logger.LogInformation(
            "[E-MAIL] Pagamento aprovado do pedido {OrderId} no valor de {Total}.",
            message.OrderId,
            message.Total);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Avisa o cliente de que o pagamento foi recusado.
/// </summary>
/// <param name="deduplicator">Deduplicador de mensagens.</param>
/// <param name="logger">Logger do consumidor.</param>
public sealed class PaymentFailedConsumer(RedisMessageDeduplicator deduplicator, ILogger<PaymentFailedConsumer> logger)
    : NotificationConsumerBase<PaymentFailedEvent>(deduplicator, logger)
{
    /// <inheritdoc />
    protected override Guid GetEventId(PaymentFailedEvent message) => message.EventId;

    /// <inheritdoc />
    protected override Task NotifyAsync(PaymentFailedEvent message)
    {
        Logger.LogWarning(
            "[E-MAIL] Pagamento recusado do pedido {OrderId}: {Reason}",
            message.OrderId,
            message.Reason);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Avisa o cliente de que o pedido foi confirmado e entrou em separacao.
/// </summary>
/// <param name="deduplicator">Deduplicador de mensagens.</param>
/// <param name="logger">Logger do consumidor.</param>
public sealed class StockReservedConsumer(RedisMessageDeduplicator deduplicator, ILogger<StockReservedConsumer> logger)
    : NotificationConsumerBase<StockReservedEvent>(deduplicator, logger)
{
    /// <inheritdoc />
    protected override Guid GetEventId(StockReservedEvent message) => message.EventId;

    /// <inheritdoc />
    protected override Task NotifyAsync(StockReservedEvent message)
    {
        Logger.LogInformation("[E-MAIL] Pedido {OrderId} confirmado e em separacao.", message.OrderId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Avisa o cliente de que o pedido foi cancelado por falta de estoque.
/// </summary>
/// <remarks>
/// A notificacao mais delicada do fluxo: o cliente ja foi cobrado. Num sistema real esta
/// mensagem informaria tambem o prazo do estorno — e este consumidor seria o gatilho
/// para abrir o chamado correspondente.
/// </remarks>
/// <param name="deduplicator">Deduplicador de mensagens.</param>
/// <param name="logger">Logger do consumidor.</param>
public sealed class StockReservationFailedConsumer(RedisMessageDeduplicator deduplicator, ILogger<StockReservationFailedConsumer> logger)
    : NotificationConsumerBase<StockReservationFailedEvent>(deduplicator, logger)
{
    /// <inheritdoc />
    protected override Guid GetEventId(StockReservationFailedEvent message) => message.EventId;

    /// <inheritdoc />
    protected override Task NotifyAsync(StockReservationFailedEvent message)
    {
        Logger.LogError(
            "[E-MAIL] Pedido {OrderId} cancelado apos o pagamento: {Reason}. Estorno necessario.",
            message.OrderId,
            message.Reason);

        return Task.CompletedTask;
    }
}
