using FluentValidation;
using Identity.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application;

/// <summary>
/// Provides dependency injection registration for the Identity application layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR, validators, and pipeline behaviors for the Identity application layer.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}