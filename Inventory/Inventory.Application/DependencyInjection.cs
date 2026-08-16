using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application;

/// <summary>
/// Registro dos servicos da camada de aplicacao do Inventory.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Ponto de extensao da camada de aplicacao do Inventory.
    /// </summary>
    /// <remarks>
    /// O Inventory nao usa MediatR: toda a sua logica e disparada por eventos, e os
    /// consumidores sao registrados diretamente no MassTransit. O metodo existe para
    /// manter o mesmo formato de composicao dos demais servicos.
    /// </remarks>
    /// <param name="services">Container de servicos.</param>
    /// <returns>O proprio <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services) => services;
}
