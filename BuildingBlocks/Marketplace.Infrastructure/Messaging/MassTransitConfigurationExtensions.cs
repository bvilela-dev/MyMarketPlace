using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Infrastructure.Messaging;

/// <summary>
/// Provides shared MassTransit bus configuration for marketplace services.
/// </summary>
public static class MassTransitConfigurationExtensions
{
    /// <summary>
    /// Configures the shared RabbitMQ transport, retry policy, circuit breaker, and endpoint conventions.
    /// </summary>
    /// <param name="configurator">The MassTransit bus configurator.</param>
    /// <param name="configuration">The application configuration source.</param>
    public static void ConfigureMarketplaceBus(this IBusRegistrationConfigurator configurator, IConfiguration configuration)
    {
        configurator.SetKebabCaseEndpointNameFormatter();
        configurator.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(configuration.GetConnectionString("RabbitMq") ?? "rabbitmq://localhost");

            cfg.UseMessageRetry(retry => retry.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(2)));
            cfg.UseCircuitBreaker(options =>
            {
                options.ActiveThreshold = 5;
                options.TrackingPeriod = TimeSpan.FromMinutes(1);
                options.ResetInterval = TimeSpan.FromMinutes(1);
                options.TripThreshold = 100;
            });

            cfg.ConfigureEndpoints(context);
        });
    }
}