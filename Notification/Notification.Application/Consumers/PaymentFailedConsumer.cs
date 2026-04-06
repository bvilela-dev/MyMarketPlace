using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Messaging.Idempotency;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Consumers;

public sealed class PaymentFailedConsumer(RedisMessageDeduplicator deduplicator, ILogger<PaymentFailedConsumer> logger) : IConsumer<PaymentFailedEvent>
{
    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var messageId = context.MessageId ?? context.Message.EventId;
        if (!await deduplicator.TryBeginAsync(messageId, nameof(PaymentFailedConsumer), context.CancellationToken))
        {
            return;
        }

        logger.LogWarning("Payment failure notification scheduled for order {OrderId}: {Reason}", context.Message.OrderId, context.Message.Reason);
    }
}