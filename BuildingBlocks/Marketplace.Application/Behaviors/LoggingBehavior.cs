using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Marketplace.Application.Behaviors;

/// <summary>
/// Registra inicio, fim e duracao de cada comando/query que passa pelo MediatR.
/// </summary>
/// <remarks>
/// <para>
/// Observabilidade barata: sem tocar em nenhum handler, todo caso de uso passa a
/// emitir um log estruturado com o tempo de execucao. Em producao isso responde
/// rapidamente a pergunta "qual caso de uso ficou lento depois do ultimo deploy?".
/// </para>
/// <para>
/// Note que os parametros do log usam <b>placeholders nomeados</b>
/// (<c>{RequestName}</c>) em vez de interpolacao de string. A diferenca e enorme:
/// o log estruturado guarda os valores como campos pesquisaveis, permitindo
/// consultas como <c>ElapsedMilliseconds &gt; 500</c> no backend de logs. Com
/// <c>$"..."</c> tudo viraria uma unica string opaca.
/// </para>
/// </remarks>
/// <typeparam name="TRequest">Tipo do comando ou query.</typeparam>
/// <typeparam name="TResponse">Tipo da resposta.</typeparam>
/// <param name="logger">Logger da categoria do behavior.</param>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        // Stopwatch.GetTimestamp() nao aloca objeto, ao contrario de Stopwatch.StartNew().
        var startedAt = Stopwatch.GetTimestamp();

        logger.LogInformation("Executando {RequestName}", requestName);

        try
        {
            var response = await next();

            logger.LogInformation(
                "{RequestName} concluido em {ElapsedMilliseconds} ms",
                requestName,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

            return response;
        }
        catch (Exception exception)
        {
            // Loga e relanca: o tratamento HTTP e responsabilidade do middleware global.
            // Engolir a excecao aqui devolveria 200 OK para um caso de uso que falhou.
            logger.LogWarning(
                exception,
                "{RequestName} falhou apos {ElapsedMilliseconds} ms",
                requestName,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

            throw;
        }
    }
}
