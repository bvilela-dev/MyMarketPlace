using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Application;

/// <summary>
/// Provides dependency injection registration for the Catalog application layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Catalog application services.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}