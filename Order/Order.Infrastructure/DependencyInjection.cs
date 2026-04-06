using Catalog.API.Grpc;
using Identity.API.Grpc;
using Grpc.Net.ClientFactory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Abstractions;
using Order.Infrastructure.Grpc;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Resilience;
using Marketplace.Infrastructure.Messaging;

namespace Order.Infrastructure;

/// <summary>
/// Provides dependency injection registration for the Order infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers persistence, gRPC clients, resilience policies, and outbox processing for Order.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <param name="configuration">The application configuration source.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrderDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Postgres")));
        services.AddScoped<IOrderDbContext>(provider => provider.GetRequiredService<OrderDbContext>());

        services.AddGrpcClient<ProductCatalog.ProductCatalogClient>(options =>
            {
                options.Address = new Uri(configuration["Grpc:Catalog"] ?? "http://localhost:5201");
            })
            .AddPolicyHandler(ResiliencePolicies.RetryPolicy())
            .AddPolicyHandler(ResiliencePolicies.CircuitBreakerPolicy());

        services.AddGrpcClient<UserValidation.UserValidationClient>(options =>
            {
                options.Address = new Uri(configuration["Grpc:Identity"] ?? "http://localhost:5101");
            })
            .AddPolicyHandler(ResiliencePolicies.RetryPolicy())
            .AddPolicyHandler(ResiliencePolicies.CircuitBreakerPolicy());

        services.AddScoped<ICatalogGrpcClient, CatalogGrpcClient>();
        services.AddScoped<IIdentityGrpcClient, IdentityGrpcClient>();
        services.AddHostedService<OutboxPublisherBackgroundService<OrderDbContext>>();
        return services;
    }
}