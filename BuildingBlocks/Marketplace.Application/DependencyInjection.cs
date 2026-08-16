using System.Reflection;
using FluentValidation;
using Marketplace.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Application;

/// <summary>
/// Registro no container das dependencias comuns da camada de aplicacao.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra MediatR, os validadores do FluentValidation e o pipeline padrao
    /// (logging + validacao) para o assembly informado.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A ordem de registro dos behaviors define a ordem de execucao.</b> O MediatR
    /// os executa na sequencia em que foram adicionados ao container, entao:
    /// </para>
    /// <code>
    /// LoggingBehavior  → mede TUDO, inclusive o tempo gasto validando
    ///   ValidationBehavior → barra dados invalidos
    ///     Handler            → regra de negocio
    /// </code>
    /// <para>
    /// Se a ordem fosse invertida, uma requisicao rejeitada por validacao nunca
    /// apareceria no log — justamente o caso que costuma interessar em investigacao.
    /// </para>
    /// </remarks>
    /// <param name="services">Container de servicos sendo configurado.</param>
    /// <param name="applicationAssembly">
    /// Assembly da camada de aplicacao do microsservico, onde estao os handlers e
    /// validadores a serem descobertos por reflexao.
    /// </param>
    /// <returns>O proprio <see cref="IServiceCollection"/>, para encadeamento.</returns>
    public static IServiceCollection AddMarketplaceApplication(this IServiceCollection services, Assembly applicationAssembly)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
