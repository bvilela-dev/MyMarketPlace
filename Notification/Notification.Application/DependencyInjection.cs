using Microsoft.Extensions.DependencyInjection;

namespace Notification.Application;

/// <summary>
/// Provides dependency injection registration for the Notification application layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Notification application services.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
        => services;
}