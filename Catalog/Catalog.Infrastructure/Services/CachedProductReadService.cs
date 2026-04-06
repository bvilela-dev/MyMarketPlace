using System.Text.Json;
using Catalog.Application.Abstractions;
using Catalog.Infrastructure.Persistence;
using Marketplace.Contracts.Grpc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Catalog.Infrastructure.Services;

/// <summary>
/// Implements a cache-aside strategy for catalog product reads.
/// </summary>
public sealed class CachedProductReadService(CatalogDbContext dbContext, IConnectionMultiplexer redis) : IProductReadService
{
    /// <summary>
    /// Retrieves a product by identifier using Redis as a read-through cache.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The product details when found; otherwise, <see langword="null"/>.</returns>
    public async Task<ProductDetailsDto?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var database = redis.GetDatabase();
        var key = $"catalog:product:{productId}";
        var cached = await database.StringGetAsync(key);
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<ProductDetailsDto>(cached!);
        }

        var product = await dbContext.Products
            .Where(candidate => candidate.Id == productId)
            .Select(candidate => new ProductDetailsDto(candidate.Id, candidate.Name, candidate.Price, candidate.AvailableQuantity))
            .FirstOrDefaultAsync(cancellationToken);

        if (product is not null)
        {
            await database.StringSetAsync(key, JsonSerializer.Serialize(product), TimeSpan.FromMinutes(10));
        }

        return product;
    }
}