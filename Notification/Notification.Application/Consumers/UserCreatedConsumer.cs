using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Messaging.Idempotency;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Consumers;

/// <summary>
/// Consumes user-created events and triggers welcome notifications.
/// </summary>
public sealed class UserCreatedConsumer(RedisMessageDeduplicator deduplicator, ILogger<UserCreatedConsumer> logger) : IConsumer<UserCreatedEvent>
{
    /// <summary>
    /// Handles the incoming user-created event.
    /// </summary>
    /// <param name="context">The MassTransit consume context.</param>
    /// <returns>A task representing the asynchronous consume operation.</returns>
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