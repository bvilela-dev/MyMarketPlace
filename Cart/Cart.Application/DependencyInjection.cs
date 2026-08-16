using Marketplace.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.Application;

/// <summary>
/// Registro dos servicos da camada de aplicacao do Cart.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra MediatR, validadores e o pipeline padrao do marketplace.
    /// </summary>
    /// <param name="services">Container de servicos.</param>
    /// <returns>O proprio <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
        => services.AddMarketplaceApplication(typeof(DependencyInjection).Assembly);
}
