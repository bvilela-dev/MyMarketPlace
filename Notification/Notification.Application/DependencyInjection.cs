using Microsoft.Extensions.DependencyInjection;

namespace Notification.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
        => services;
}