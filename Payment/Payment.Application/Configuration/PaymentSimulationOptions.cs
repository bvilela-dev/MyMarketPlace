namespace Payment.Application.Configuration;

/// <summary>
/// Parametros da simulacao de pagamento, lidos da secao <c>"PaymentSimulation"</c>.
/// </summary>
/// <remarks>
/// Antes esses valores eram constantes cravadas no consumidor. Externalizar permite
/// demonstrar o caminho de falha do saga apenas baixando o limite no appsettings — sem
/// recompilar nada.
/// </remarks>
public sealed class PaymentSimulationOptions
{
    /// <summary>
    /// Nome da secao de configuracao correspondente.
    /// </summary>
    public const string SectionName = "PaymentSimulation";

    /// <summary>
    /// Valor maximo aprovado automaticamente. Acima disso, o pagamento e recusado.
    /// </summary>
    public decimal ApprovalLimit { get; set; } = 10_000m;

    /// <summary>
    /// Atraso artificial antes de decidir, para tornar o fluxo assincrono visivel.
    /// </summary>
    public TimeSpan SimulatedLatency { get; set; } = TimeSpan.FromMilliseconds(250);
}
