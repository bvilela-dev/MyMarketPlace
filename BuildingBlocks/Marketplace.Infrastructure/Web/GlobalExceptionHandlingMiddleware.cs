using System.Diagnostics;
using FluentValidation;
using Marketplace.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Marketplace.Infrastructure.Web;

/// <summary>
/// Converte qualquer excecao nao tratada numa resposta HTTP padronizada
/// (<c>application/problem+json</c>, RFC 9457).
/// </summary>
/// <remarks>
/// <para>
/// <b>Por que centralizar?</b> Sem este middleware cada controller precisaria de
/// try/catch, e a API responderia formatos diferentes conforme quem escreveu o
/// endpoint. Aqui existe um unico lugar que define o contrato de erro da API inteira.
/// </para>
/// <para>
/// <b>Mapeamento aplicado:</b>
/// </para>
/// <list type="table">
///   <listheader><term>Excecao</term><description>Status HTTP</description></listheader>
///   <item><term><see cref="ValidationException"/></term><description>400 — payload malformado, com a lista de campos invalidos</description></item>
///   <item><term><see cref="AuthenticationFailedException"/></term><description>401 — credenciais invalidas ou expiradas</description></item>
///   <item><term><see cref="ForbiddenAccessException"/></term><description>403 — autenticado, mas o recurso e de outro usuario</description></item>
///   <item><term><see cref="NotFoundException"/></term><description>404 — recurso inexistente</description></item>
///   <item><term><see cref="BusinessRuleException"/></term><description>409 — regra de negocio violada</description></item>
///   <item><term><see cref="OperationCanceledException"/></term><description>499 — cliente desistiu da requisicao</description></item>
///   <item><term>qualquer outra</term><description>500 — bug ou indisponibilidade</description></item>
/// </list>
/// <para>
/// <b>Tres detalhes que a versao anterior deste middleware nao tinha:</b>
/// </para>
/// <list type="number">
///   <item>Existia um <c>catch (InvalidOperationException)</c> devolvendo 400. Como o
///   proprio .NET lanca esse tipo para erros de infraestrutura, bugs reais eram
///   entregues ao cliente como "erro de validacao" e sumiam do radar.</item>
///   <item>Nao havia <c>catch</c> generico: excecoes fora da lista vazavam para o
///   Kestrel, que respondia HTML — quebrando qualquer cliente que esperasse JSON.</item>
///   <item>Nada era registrado em log. Agora um 5xx sempre gera <c>LogError</c>, e a
///   resposta carrega o <c>traceId</c> que liga o erro visto pelo cliente ao trace
///   correspondente no OpenTelemetry.</item>
/// </list>
/// </remarks>
/// <param name="next">Proximo middleware do pipeline.</param>
/// <param name="logger">Logger usado para registrar as falhas.</param>
/// <param name="environment">Ambiente de execucao, usado para decidir o nivel de detalhe do 500.</param>
public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    /// <summary>
    /// Status code nao oficial (convencao do nginx) para "cliente fechou a conexao".
    /// </summary>
    private const int ClientClosedRequest = 499;

    /// <summary>
    /// Executa o middleware para a requisicao atual.
    /// </summary>
    /// <param name="context">Contexto HTTP da requisicao.</param>
    /// <returns>Task da execucao assincrona.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        // Se a resposta ja comecou a ser enviada (headers no fio), nao ha como trocar o
        // status code. Tentar escrever aqui geraria uma segunda excecao, mascarando a
        // original. O certo e apenas registrar e deixar a conexao ser encerrada.
        if (context.Response.HasStarted)
        {
            logger.LogError(exception, "Excecao apos o inicio do envio da resposta; nao foi possivel formatar o erro.");
            return;
        }

        var problem = BuildProblemDetails(context, exception);

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Falha nao tratada em {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            // 4xx e comportamento esperado da API, nao incidente: fica em Debug para
            // nao poluir o log de producao com erro de digitacao do cliente.
            logger.LogDebug(exception, "Requisicao rejeitada em {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, contentType: "application/problem+json");
    }

    private ProblemDetails BuildProblemDetails(HttpContext context, Exception exception)
    {
        // O traceId correlaciona a resposta de erro com o trace distribuido exportado
        // via OTLP. Com ele, o suporte pega o id devolvido ao cliente e abre exatamente
        // a mesma requisicao no Grafana/Jaeger.
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        ProblemDetails problem = exception switch
        {
            ValidationException validationException => new ValidationProblemDetails(
                validationException.Errors
                    .GroupBy(failure => failure.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Um ou mais campos sao invalidos.",
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1"
            },

            AuthenticationFailedException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Nao autenticado.",
                Detail = exception.Message,
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2"
            },

            ForbiddenAccessException => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Acesso negado.",
                Detail = exception.Message,
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4"
            },

            NotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Recurso nao encontrado.",
                Detail = exception.Message,
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5"
            },

            BusinessRuleException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Regra de negocio violada.",
                Detail = exception.Message,
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10"
            },

            // Cancelamento nao e falha: normalmente o usuario fechou a aba ou o gateway
            // atingiu o timeout. Nao deve contar como erro nos alertas de 5xx.
            OperationCanceledException when context.RequestAborted.IsCancellationRequested => new ProblemDetails
            {
                Status = ClientClosedRequest,
                Title = "Requisicao cancelada pelo cliente."
            },

            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erro interno inesperado.",
                // Detalhe tecnico so em desenvolvimento. Em producao a mensagem de uma
                // excecao pode conter connection string, caminho de arquivo ou nome de
                // tabela — informacao util para quem esta atacando o sistema.
                Detail = environment.IsDevelopment() ? exception.ToString() : "Contate o suporte informando o traceId.",
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1"
            }
        };

        problem.Instance = $"{context.Request.Method} {context.Request.Path}";
        problem.Extensions["traceId"] = traceId;

        return problem;
    }
}
