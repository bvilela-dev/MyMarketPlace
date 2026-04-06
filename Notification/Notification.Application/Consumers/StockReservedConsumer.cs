using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Messaging.Idempotency;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Consumers;

public sealed class StockReservedConsumer(RedisMessageDeduplicator deduplicator, ILogger<StockReservedConsumer> logger) : IConsumer<StockReservedEvent>
{
    public async Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        var messageId = context.MessageId ?? context.Message.EventId;
        if (!await deduplicator.TryBeginAsync(messageId, nameof(StockReservedConsumer), context.CancellationToken))
        {
            return;
        }

        logger.LogInformation("Shipment confirmation notification scheduled for order {OrderId}", context.Message.OrderId);
    }
}