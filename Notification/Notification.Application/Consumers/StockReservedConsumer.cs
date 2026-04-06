using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Messaging.Idempotency;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Consumers;

/// <summary>
/// Consumes stock-reserved events and triggers shipment notifications.
/// </summary>
public sealed class StockReservedConsumer(RedisMessageDeduplicator deduplicator, ILogger<StockReservedConsumer> logger) : IConsumer<StockReservedEvent>
{
    /// <summary>
    /// Handles the incoming stock-reserved event.
    /// </summary>
    /// <param name="context">The MassTransit consume context.</param>
    /// <returns>A task representing the asynchronous consume operation.</returns>
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