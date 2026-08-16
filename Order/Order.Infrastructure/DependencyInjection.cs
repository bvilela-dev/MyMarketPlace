using Catalog.API.Grpc;
using Identity.API.Grpc;
using Marketplace.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Abstractions;
using Order.Infrastructure.Grpc;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Resilience;

namespace Order.Infrastructure;

/// <summary>
/// Registro dos servicos de infraestrutura do Order.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra persistencia, clientes gRPC resilientes e o publicador de outbox.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Sobre os namespaces <c>Catalog.API.Grpc</c> e <c>Identity.API.Grpc</c>:</b> eles
    /// nao vem de uma referencia a esses projetos — isso quebraria o isolamento entre
    /// microsservicos. Vem das classes que o compilador do protobuf gera a partir dos
    /// arquivos <c>.proto</c> ligados no <c>.csproj</c> deste projeto, e o namespace e
    /// simplesmente o declarado dentro do <c>.proto</c> de origem.
    /// </para>
    /// <para>
    /// O que existe aqui e um acoplamento a <b>contratos</b>, verificado em tempo de
    /// compilacao — que e exatamente o beneficio que se busca ao adotar gRPC.
    /// </para>
    /// </remarks>
    /// <param name="services">Container de servicos.</param>
    /// <param name="configuration">Fonte de configuracao.</param>
    /// <returns>O proprio <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrderDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null)));

        services.AddScoped<IOrderDbContext>(provider => provider.GetRequiredService<OrderDbContext>());

        AddResilientGrpcClient<ProductCatalog.ProductCatalogClient>(services, configuration["Grpc:Catalog"] ?? "http://localhost:5200");
        AddResilientGrpcClient<UserValidation.UserValidationClient>(services, configuration["Grpc:Identity"] ?? "http://localhost:5100");

        services.AddScoped<ICatalogGrpcClient, CatalogGrpcClient>();
        services.AddScoped<IIdentityGrpcClient, IdentityGrpcClient>();

        services.AddHostedService<OutboxPublisherBackgroundService<OrderDbContext>>();

        return services;
    }

    /// <summary>
    /// Registra um cliente gRPC com retry e circuit breaker.
    /// </summary>
    /// <remarks>
    /// A ordem das politicas importa: <c>AddPolicyHandler</c> monta o pipeline de fora
    /// para dentro, entao a primeira registrada e a mais externa.
    /// <code>
    /// CircuitBreaker  (mais externo — corta tudo quando o servico esta fora)
    ///   └─ Retry      (repete falhas momentaneas)
    ///        └─ chamada HTTP/2 real
    /// </code>
    /// Assim, com o circuito aberto nem o retry chega a ser executado — que e
    /// justamente o comportamento desejado quando o destino esta indisponivel.
    /// </remarks>
    private static void AddResilientGrpcClient<TClient>(IServiceCollection services, string address)
        where TClient : class
    {
        services.AddGrpcClient<TClient>(options => options.Address = new Uri(address))
            .AddPolicyHandler(ResiliencePolicies.CircuitBreakerPolicy())
            .AddPolicyHandler(ResiliencePolicies.RetryPolicy());
    }
}
