using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Marketplace.Infrastructure.Web.HealthChecks;

/// <summary>
/// Verifica se o Redis responde, usando o comando <c>PING</c>.
/// </summary>
/// <remarks>
/// Health check escrito a mao, sem pacote de terceiros, justamente porque a checagem
/// util e trivial: se o <c>PING</c> volta, a conexao esta viva e autenticada.
/// </remarks>
/// <param name="connectionMultiplexer">Conexao compartilhada com o Redis.</param>
public sealed class RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await connectionMultiplexer.GetDatabase().PingAsync();

            return HealthCheckResult.Healthy(
                "Redis respondendo.",
                new Dictionary<string, object> { ["latencyMs"] = latency.TotalMilliseconds });
        }
        catch (Exception exception)
        {
            // Devolver Unhealthy (em vez de deixar a excecao subir) e o que permite ao
            // endpoint /health/ready responder 503 com o motivo, em vez de estourar 500.
            return HealthCheckResult.Unhealthy("Redis inacessivel.", exception);
        }
    }
}
