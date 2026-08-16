using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Marketplace.Infrastructure.Observability;

/// <summary>
/// Configuracao unica de OpenTelemetry para todos os microsservicos.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por que OpenTelemetry?</b> Num monolito, uma stack trace conta a historia
/// inteira. Aqui, um unico "criar pedido" atravessa Gateway → Order → (gRPC) Identity
/// → (gRPC) Catalog → RabbitMQ → Payment → Inventory → Notification. Sem correlacao,
/// investigar lentidao vira arqueologia em sete arquivos de log.
/// </para>
/// <para>
/// <b>Os tres sinais configurados aqui:</b>
/// </para>
/// <list type="bullet">
///   <item><b>Traces</b> — a linha do tempo de uma requisicao atravessando os servicos.
///   Responde "onde foram os 800 ms?".</item>
///   <item><b>Metricas</b> — numeros agregados ao longo do tempo (taxa de requisicoes,
///   latencia p99, GC, threads). Responde "esta piorando?".</item>
///   <item><b>Logs</b> — o evento textual, aqui exportado ja carregando
///   <c>TraceId</c>/<c>SpanId</c>, o que permite pular do log direto para o trace.</item>
/// </list>
/// <para>
/// <b>Como a correlacao atravessa os processos?</b> Via cabecalho W3C
/// <c>traceparent</c>, propagado automaticamente pelas instrumentacoes de
/// ASP.NET Core, HttpClient e MassTransit. Por isso o trace continua o mesmo depois
/// de passar pela fila.
/// </para>
/// </remarks>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Registra traces, metricas e logs do OpenTelemetry para um servico.
    /// </summary>
    /// <param name="services">Container de servicos.</param>
    /// <param name="configuration">Fonte de configuracao (le <c>OpenTelemetry:OtlpEndpoint</c>).</param>
    /// <param name="serviceName">
    /// Nome logico do servico exportado na telemetria (ex.: <c>"order-service"</c>).
    /// E por ele que os spans sao agrupados por servico no Grafana/Jaeger.
    /// </param>
    /// <returns>O proprio <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMarketplaceTelemetry(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
        var hasExporter = !string.IsNullOrWhiteSpace(otlpEndpoint);

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: typeof(OpenTelemetryExtensions).Assembly.GetName().Version?.ToString())
            // service.instance.id distingue as replicas do mesmo servico. Sem ele, um pod
            // doente entre cinco replicas some na media e o problema fica invisivel.
            .AddAttributes([new KeyValuePair<string, object>("service.instance.id", Environment.MachineName)]);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName)
                .AddAttributes([new KeyValuePair<string, object>("service.instance.id", Environment.MachineName)]))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Health check roda a cada poucos segundos em cada pod. Sem este
                        // filtro, mais de 90% dos spans exportados seriam ruido de sonda.
                        options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    // Fonte de spans do MassTransit: e o que costura o trace atraves do
                    // RabbitMQ, ligando quem publicou a quem consumiu.
                    .AddSource("MassTransit");

                if (hasExporter)
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint!));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    // Metricas de runtime: GC, heap, thread pool, excecoes. Sao elas que
                    // revelam vazamento de memoria e starvation do thread pool.
                    .AddRuntimeInstrumentation();

                if (hasExporter)
                {
                    metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint!));
                }
            });

        services.AddLogging(logging => logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(resourceBuilder);
            // Sem estas duas linhas o log exportado perde o escopo e a mensagem original
            // formatada — e some justamente a informacao que se quer ler no incidente.
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;

            if (hasExporter)
            {
                options.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint!));
            }
        }));

        return services;
    }
}
