using Cart.Application.Abstractions;
using Cart.Infrastructure.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Cart.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379"));
        services.AddScoped<ICartStore, RedisCartStore>();
        return services;
    }
}