using Inventory.Application.Persistence;
using Marketplace.Infrastructure.Messaging.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Inventory.Infrastructure;

/// <summary>
/// Provides dependency injection registration for the Inventory infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Inventory persistence and Redis-backed idempotency services.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <param name="configuration">The application configuration source.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<Inventory.Infrastructure.Persistence.InventoryDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Postgres")));
        services.AddScoped<IInventoryRepository, Inventory.Infrastructure.Persistence.InventoryRepository>();
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379"));
        services.AddSingleton<RedisMessageDeduplicator>();
        return services;
    }
}