using Identity.Application.Abstractions;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Security;
using Marketplace.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

/// <summary>
/// Registro dos servicos de infraestrutura do Identity.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra persistencia, seguranca e o publicador de outbox do Identity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Sobre os tempos de vida escolhidos:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><c>DbContext</c> — <b>Scoped</b> (um por requisicao). Ele nao e
    ///   thread-safe e mantem o change tracker; compartilhar entre requisicoes
    ///   causaria corrupcao de estado.</item>
    ///   <item><c>IPasswordHasher</c> e <c>ITokenService</c> — sem estado, poderiam ser
    ///   Singleton. Ficam Scoped por consistencia e porque o custo e irrelevante.</item>
    ///   <item><c>OutboxPublisherBackgroundService</c> — <b>Singleton</b> (todo
    ///   <c>IHostedService</c> e). Por isso ele abre um escopo proprio a cada ciclo para
    ///   resolver o DbContext.</item>
    /// </list>
    /// <para>
    /// <b>A autenticacao JWT nao e registrada aqui</b>: fica em
    /// <c>AddMarketplaceJwtAuthentication</c>, chamada no <c>Program.cs</c>, para que
    /// Identity, Cart e Order compartilhem exatamente a mesma configuracao de validacao.
    /// </para>
    /// </remarks>
    /// <param name="services">Container de servicos.</param>
    /// <param name="configuration">Fonte de configuracao.</param>
    /// <returns>O proprio <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null)));

        services.AddScoped<IIdentityDbContext>(provider => provider.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        services.AddHostedService<OutboxPublisherBackgroundService<IdentityDbContext>>();

        return services;
    }
}
