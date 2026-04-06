using StackExchange.Redis;

namespace Marketplace.Infrastructure.Messaging.Idempotency;

public sealed class RedisMessageDeduplicator(IConnectionMultiplexer connectionMultiplexer)
{
    public async Task<bool> TryBeginAsync(Guid messageId, string consumerName, CancellationToken cancellationToken = default)
    {
        var database = connectionMultiplexer.GetDatabase();
        var key = $"consumer:{consumerName}:message:{messageId}";

        return await database.StringSetAsync(key, DateTime.UtcNow.ToString("O"), TimeSpan.FromDays(7), When.NotExists);
    }
}