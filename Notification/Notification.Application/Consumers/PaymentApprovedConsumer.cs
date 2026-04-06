using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Messaging.Idempotency;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Consumers;

public sealed class PaymentApprovedConsumer(RedisMessageDeduplicator deduplicator, ILogger<PaymentApprovedConsumer> logger) : IConsumer<PaymentApprovedEvent>
{
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