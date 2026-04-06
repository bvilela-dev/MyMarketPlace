using System.Text.Json;
using Cart.Application.Abstractions;
using Cart.Domain.Entities;
using StackExchange.Redis;

namespace Cart.Infrastructure.Redis;

public sealed class RedisCartStore(IConnectionMultiplexer connectionMultiplexer) : ICartStore
{
    public async Task<ShoppingCart?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var database = connectionMultiplexer.GetDatabase();
        var payload = await database.StringGetAsync($"cart:{userId}");
        return payload.HasValue ? JsonSerializer.Deserialize<ShoppingCart>(payload!) : null;
    }

    public Task SaveAsync(ShoppingCart cart, CancellationToken cancellationToken = default)
    {
        var database = connectionMultiplexer.GetDatabase();
        return database.StringSetAsync($"cart:{cart.UserId}", JsonSerializer.Serialize(cart), TimeSpan.FromDays(7));
    }
}