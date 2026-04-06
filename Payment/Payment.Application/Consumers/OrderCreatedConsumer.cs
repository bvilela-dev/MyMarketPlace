using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Messaging.Idempotency;
using MassTransit;

namespace Payment.Application.Consumers;

public sealed class OrderCreatedConsumer(RedisMessageDeduplicator deduplicator) : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var messageId = context.MessageId ?? context.Message.EventId;
        if (!await deduplicator.TryBeginAsync(messageId, nameof(OrderCreatedConsumer), context.CancellationToken))
        {
            return;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(250), context.CancellationToken);

        if (context.Message.Total <= 10_000m)
        {
            await context.Publish(new PaymentApprovedEvent(Guid.NewGuid(), context.Message.OrderId, context.Message.UserId, context.Message.Total, DateTime.UtcNow));
            return;
        }

        await context.Publish(new PaymentFailedEvent(Guid.NewGuid(), context.Message.OrderId, context.Message.UserId, "Payment amount exceeded simulated limit.", DateTime.UtcNow));
    }
}