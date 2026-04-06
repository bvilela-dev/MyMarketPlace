using System.Text.Json;
using Cart.Application.Abstractions;
using Cart.Domain.Entities;
using StackExchange.Redis;

namespace Cart.Infrastructure.Redis;

/// <summary>
/// Stores full shopping carts in Redis.
/// </summary>
public sealed class RedisCartStore(IConnectionMultiplexer connectionMultiplexer) : ICartStore
{
    /// <summary>
    /// Retrieves the cart for a user from Redis.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The shopping cart when found; otherwise, <see langword="null"/>.</returns>
    public async Task<ShoppingCart?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var database = connectionMultiplexer.GetDatabase();
        var payload = await database.StringGetAsync($"cart:{userId}");
        return payload.HasValue ? JsonSerializer.Deserialize<ShoppingCart>(payload!) : null;
    }

    /// <summary>
    /// Saves the cart to Redis.
    /// </summary>
    /// <param name="cart">The cart to persist.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    public Task SaveAsync(ShoppingCart cart, CancellationToken cancellationToken = default)
    {
        var database = connectionMultiplexer.GetDatabase();
        return database.StringSetAsync($"cart:{cart.UserId}", JsonSerializer.Serialize(cart), TimeSpan.FromDays(7));
    }
}