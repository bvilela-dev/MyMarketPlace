using Inventory.Application.Persistence;
using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Messaging.Idempotency;
using MassTransit;

namespace Inventory.Application.Consumers;

public sealed class PaymentApprovedConsumer(IInventoryRepository inventoryRepository, RedisMessageDeduplicator deduplicator) : IConsumer<PaymentApprovedEvent>
{
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