using Microsoft.Extensions.DependencyInjection;

namespace Notification.Application;

/// <summary>
/// Registro dos servicos da camada de aplicacao do Notification.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Ponto de extensao da camada de aplicacao do Notification.
    /// </summary>
    /// <remarks>
    /// Servico puramente reativo: os consumidores sao registrados no MassTransit e nao
    /// ha caso de uso sincrono. O metodo mantem o formato de composicao dos demais.
    /// </remarks>
    /// <param name="services">Container de servicos.</param>
    /// <returns>O proprio <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services) => services;
}
