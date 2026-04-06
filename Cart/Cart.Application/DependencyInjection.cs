using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.Application;

/// <summary>
/// Provides dependency injection registration for the Cart application layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR handlers for the Cart application layer.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}