using StackExchange.Redis;

namespace Marketplace.Infrastructure.Messaging.Idempotency;

/// <summary>
/// Provides Redis-backed message deduplication for idempotent consumers.
/// </summary>
public sealed class RedisMessageDeduplicator(IConnectionMultiplexer connectionMultiplexer)
{
    /// <summary>
    /// Attempts to mark a message as being processed by a consumer.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="consumerName">The consumer name.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns><see langword="true"/> when processing can start; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> TryBeginAsync(Guid messageId, string consumerName, CancellationToken cancellationToken = default)
    {
        var database = connectionMultiplexer.GetDatabase();
        var key = $"consumer:{consumerName}:message:{messageId}";

        return await database.StringSetAsync(key, DateTime.UtcNow.ToString("O"), TimeSpan.FromDays(7), When.NotExists);
    }
}