using Cart.Application.Abstractions;
using Cart.Infrastructure.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Cart.Infrastructure;

/// <summary>
/// Registro dos servicos de infraestrutura do Cart.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra a conexao Redis e o armazenamento do carrinho.
    /// </summary>
    /// <param name="services">Container de servicos.</param>
    /// <param name="configuration">Fonte de configuracao.</param>
    /// <returns>O proprio <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(configuration.GetConnectionString("Redis") ?? "localhost:6379");
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 5;
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddScoped<ICartStore, RedisCartStore>();

        return services;
    }
}
