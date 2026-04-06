using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Infrastructure.Messaging;

public static class MassTransitConfigurationExtensions
{
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