using Polly;
using Polly.Extensions.Http;

namespace Order.Infrastructure.Resilience;

/// <summary>
/// Politicas de resiliencia das chamadas de saida do Order.
/// </summary>
/// <remarks>
/// <para>
/// O Order depende de Identity e Catalog por gRPC. Rede falha, pod reinicia, deploy
/// acontece — sem protecao, qualquer soluco vira erro para o cliente final.
/// </para>
/// <para>
/// <b>Retry e circuit breaker resolvem problemas diferentes e se complementam:</b>
/// </para>
/// <list type="bullet">
///   <item><b>Retry</b> parte do princípio de que a falha e <i>momentanea</i> — tentar de
///   novo em um segundo deve funcionar.</item>
///   <item><b>Circuit breaker</b> parte do princípio oposto: o servico esta <i>fora do
///   ar</i>, e insistir so piora. Depois de N falhas ele "abre" e passa a falhar
///   imediatamente, sem nem tentar a rede.</item>
/// </list>
/// <para>
/// <b>Sem o circuit breaker, o retry vira o problema.</b> Se o Catalog cai, cada
/// requisicao gera 4 chamadas (1 + 3 tentativas) e cada uma prende uma thread esperando
/// timeout. O Order esgota o pool de conexoes e cai junto — e assim uma falha isolada
/// vira uma queda em cascata.
/// </para>
/// <para>
/// <b>Jitter no retry.</b> Se 500 requisicoes falham no mesmo instante e todas repetem
/// exatamente 2 segundos depois, o servico que estava se recuperando leva outra rajada
/// simultanea. O componente aleatorio espalha as tentativas no tempo — e o
/// "thundering herd" que a AWS documenta em seus guias de arquitetura.
/// </para>
/// </remarks>
public static class ResiliencePolicies
{
    /// <summary>
    /// Politica de retry com backoff exponencial e jitter.
    /// </summary>
    /// <remarks>
    /// Progressao aproximada: 2s, 4s, 8s — cada uma somada a ate 1 segundo aleatorio.
    /// <para>
    /// <c>HandleTransientHttpError</c> cobre 5xx, 408 e falhas de socket. Note que ele
    /// <b>nao</b> repete 4xx: reenviar uma requisicao malformada daria o mesmo erro.
    /// </para>
    /// </remarks>
    /// <returns>Politica de retry.</returns>
    public static IAsyncPolicy<HttpResponseMessage> RetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)) +
                    TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1_000)));

    /// <summary>
    /// Politica de circuit breaker.
    /// </summary>
    /// <remarks>
    /// Abre depois de 5 falhas consecutivas e permanece aberto por 30 segundos. Ao
    /// fechar, o Polly deixa passar uma chamada de teste (estado <i>half-open</i>): se
    /// funcionar, o circuito fecha; se falhar, abre novamente.
    /// <para>
    /// 30 segundos e curto o bastante para a recuperacao ser rapida e longo o bastante
    /// para dar folga ao servico que esta reiniciando.
    /// </para>
    /// </remarks>
    /// <returns>Politica de circuit breaker.</returns>
    public static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
}
