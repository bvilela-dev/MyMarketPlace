using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Marketplace.Infrastructure.Observability;

/// <summary>
/// Provides OpenTelemetry registration helpers for marketplace services.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Registers the shared OpenTelemetry tracing configuration for a service.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <param name="configuration">The application configuration source.</param>
    /// <param name="serviceName">The logical service name exported to telemetry backends.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMarketplaceTelemetry(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                }
            });

        return services;
    }
}