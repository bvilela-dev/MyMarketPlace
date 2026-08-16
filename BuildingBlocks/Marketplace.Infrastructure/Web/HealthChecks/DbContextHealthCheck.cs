using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Marketplace.Infrastructure.Web.HealthChecks;

/// <summary>
/// Verifica se o banco relacional de um <typeparamref name="TDbContext"/> esta acessivel.
/// </summary>
/// <remarks>
/// Usa <c>CanConnectAsync</c>, que apenas abre a conexao — nao executa consulta na
/// aplicacao. Um health check nunca deve ser caro: ele roda a cada poucos segundos em
/// cada pod, e uma consulta pesada aqui viraria carga extra justamente quando o
/// sistema ja esta sob pressao.
/// </remarks>
/// <typeparam name="TDbContext">Tipo do contexto do EF Core a ser verificado.</typeparam>
/// <param name="dbContext">Instancia do contexto resolvida pelo container.</param>
public sealed class DbContextHealthCheck<TDbContext>(TDbContext dbContext) : IHealthCheck
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy($"{typeof(TDbContext).Name} conectado.")
                : HealthCheckResult.Unhealthy($"{typeof(TDbContext).Name} nao conseguiu conectar.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy($"{typeof(TDbContext).Name} inacessivel.", exception);
        }
    }
}
