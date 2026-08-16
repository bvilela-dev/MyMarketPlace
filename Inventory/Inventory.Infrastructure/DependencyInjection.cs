using Inventory.Application.Persistence;
using Inventory.Infrastructure.Persistence;
using Marketplace.Infrastructure.Messaging.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Inventory.Infrastructure;

/// <summary>
/// Registro dos servicos de infraestrutura do Inventory.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra persistencia e idempotencia do Inventory.
    /// </summary>
    /// <param name="services">Container de servicos.</param>
    /// <param name="configuration">Fonte de configuracao.</param>
    /// <returns>O proprio <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null)));

        services.AddScoped<IInventoryRepository, InventoryRepository>();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(configuration.GetConnectionString("Redis") ?? "localhost:6379");
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 5;
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<RedisMessageDeduplicator>();

        return services;
    }
}
