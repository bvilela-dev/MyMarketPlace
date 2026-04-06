using Inventory.Application.Persistence;
using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Messaging.Idempotency;
using MassTransit;

namespace Inventory.Application.Consumers;

/// <summary>
/// Consumes payment-approved events and reserves stock.
/// </summary>
public sealed class PaymentApprovedConsumer(IInventoryRepository inventoryRepository, RedisMessageDeduplicator deduplicator) : IConsumer<PaymentApprovedEvent>
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

        await inventoryRepository.ReserveAsync(context.Message.OrderId, context.CancellationToken);
        await context.Publish(new StockReservedEvent(Guid.NewGuid(), context.Message.OrderId, context.Message.UserId, DateTime.UtcNow));
    }
}