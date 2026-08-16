using Marketplace.Infrastructure.Messaging.Idempotency;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Payment.Infrastructure;

/// <summary>
/// Registro dos servicos de infraestrutura do Payment.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra a conexao Redis e o deduplicador de mensagens.
    /// </summary>
    /// <remarks>
    /// O Payment nao tem banco proprio nesta versao — e um consumidor sem estado. Num
    /// sistema real teria a sua propria base de transacoes, com o identificador da
    /// autorizacao junto ao adquirente.
    /// </remarks>
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

        services.AddSingleton<RedisMessageDeduplicator>();

        return services;
    }
}
