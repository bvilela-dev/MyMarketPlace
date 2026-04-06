using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Messaging.Idempotency;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Consumers;

public sealed class UserCreatedConsumer(RedisMessageDeduplicator deduplicator, ILogger<UserCreatedConsumer> logger) : IConsumer<UserCreatedEvent>
{
    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var messageId = context.MessageId ?? context.Message.EventId;
        if (!await deduplicator.TryBeginAsync(messageId, nameof(UserCreatedConsumer), context.CancellationToken))
        {
            return;
        }

        logger.LogInformation("Welcome email scheduled for user {Email}", context.Message.Email);
    }
}