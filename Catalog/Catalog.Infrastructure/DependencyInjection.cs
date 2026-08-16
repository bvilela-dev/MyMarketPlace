using Catalog.Application.Abstractions;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Services;
using Marketplace.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Catalog.Infrastructure;

/// <summary>
/// Registro dos servicos de infraestrutura do Catalog.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra persistencia, cache e o publicador de outbox do Catalog.
    /// </summary>
    /// <param name="services">Container de servicos.</param>
    /// <param name="configuration">Fonte de configuracao.</param>
    /// <returns>O proprio <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null)));

        services.AddScoped<ICatalogDbContext>(provider => provider.GetRequiredService<CatalogDbContext>());
        services.AddSingleton<IConnectionMultiplexer>(_ => CreateRedisConnection(configuration));
        services.AddScoped<IProductReadService, CachedProductReadService>();

        services.AddHostedService<OutboxPublisherBackgroundService<CatalogDbContext>>();

        return services;
    }

    /// <summary>
    /// Abre a conexao com o Redis com politica de reconexao.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>IConnectionMultiplexer</c> e <b>Singleton</b> por exigencia da biblioteca: ele
    /// ja multiplexa todos os comandos sobre poucas conexoes TCP. Cria-lo por requisicao
    /// (o erro mais comum com StackExchange.Redis) esgota o pool de sockets do servidor.
    /// </para>
    /// <para>
    /// <c>AbortOnConnectFail = false</c> e essencial em container: sem isso, se o Redis
    /// ainda estiver subindo quando a API iniciar, a conexao falha de vez e so um restart
    /// do pod resolve. Com <see langword="false"/>, a biblioteca reconecta sozinha.
    /// </para>
    /// </remarks>
    private static IConnectionMultiplexer CreateRedisConnection(IConfiguration configuration)
    {
        var options = ConfigurationOptions.Parse(configuration.GetConnectionString("Redis") ?? "localhost:6379");
        options.AbortOnConnectFail = false;
        options.ConnectRetry = 5;
        options.ConnectTimeout = 5_000;

        return ConnectionMultiplexer.Connect(options);
    }
}
