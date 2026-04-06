using Catalog.Application.Abstractions;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Catalog.Infrastructure;

/// <summary>
/// Provides dependency injection registration for the Catalog infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Catalog persistence and caching services.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <param name="configuration">The application configuration source.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CatalogDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Postgres")));
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379"));
        services.AddScoped<IProductReadService, CachedProductReadService>();
        return services;
    }
}