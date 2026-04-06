using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Messaging.Idempotency;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Consumers;

/// <summary>
/// Consumes payment-approved events and triggers payment confirmation notifications.
/// </summary>
public sealed class PaymentApprovedConsumer(RedisMessageDeduplicator deduplicator, ILogger<PaymentApprovedConsumer> logger) : IConsumer<PaymentApprovedEvent>
{
    /// <summary>
    /// Handles the incoming payment-approved event.
    /// </summary>
    /// <param name="context">The MassTransit consume context.</param>
    /// <returns>A task representing the asynchronous consume operation.</returns>
    public async Task Consume(ConsumeContext<PaymentApprovedEvent> context)
    {
        var messageId = context.MessageId ?? context.Message.EventId;
        if (!await deduplicator.TryBeginAsync(messageId, nameof(PaymentApprovedConsumer), context.CancellationToken))
        {
            return;
        }

        logger.LogInformation("Payment approved notification scheduled for order {OrderId}", context.Message.OrderId);
    }
}