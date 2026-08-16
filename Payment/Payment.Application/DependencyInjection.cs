using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Configuration;

namespace Payment.Application;

/// <summary>
/// Registro dos servicos da camada de aplicacao do Payment.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra as opcoes da simulacao de pagamento.
    /// </summary>
    /// <remarks>
    /// O Payment nao usa MediatR: ele nao expoe casos de uso por HTTP, apenas reage a
    /// eventos. Adicionar o mediador aqui seria cerimonia sem beneficio.
    /// </remarks>
    /// <param name="services">Container de servicos.</param>
    /// <param name="configuration">Fonte de configuracao.</param>
    /// <returns>O proprio <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PaymentSimulationOptions>(configuration.GetSection(PaymentSimulationOptions.SectionName));
        return services;
    }
}
