using Marketplace.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application;

/// <summary>
/// Registro dos servicos da camada de aplicacao do Identity.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra MediatR, validadores e o pipeline padrao do marketplace.
    /// </summary>
    /// <remarks>
    /// Toda a configuracao vem de <c>AddMarketplaceApplication</c>, no building block
    /// compartilhado. O que o servico informa e apenas <i>qual assembly</i> varrer em
    /// busca de handlers e validadores.
    /// </remarks>
    /// <param name="services">Container de servicos.</param>
    /// <returns>O proprio <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
        => services.AddMarketplaceApplication(typeof(DependencyInjection).Assembly);
}
