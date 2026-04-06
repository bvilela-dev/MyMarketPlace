using Marketplace.Infrastructure.Messaging.Idempotency;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Notification.Infrastructure;

/// <summary>
/// Provides dependency injection registration for the Notification infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Redis-backed infrastructure services used by Notification consumers.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <param name="configuration">The application configuration source.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379"));
        services.AddSingleton<RedisMessageDeduplicator>();
        return services;
    }
}