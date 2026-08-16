using System.Text.Json;
using MassTransit;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Marketplace.Infrastructure.Messaging;

/// <summary>
/// Servico em background que publica no barramento os eventos pendentes do outbox.
/// </summary>
/// <remarks>
/// <para>
/// E a segunda metade do padrao Outbox (a primeira e o <see cref="IntegrationEventOutboxWriter{TDbContext}"/>,
/// que grava). O ciclo e simples:
/// </para>
/// <code>
/// a cada 5s:
///   1. seleciona ate N mensagens com ProcessedOnUtc == null, mais antigas primeiro
///   2. desserializa e publica cada uma no RabbitMQ (via MassTransit)
///   3. marca ProcessedOnUtc = agora
///   4. salva
/// </code>
/// <para>
/// <b>Detalhes que separam um outbox de brinquedo de um de producao:</b>
/// </para>
/// <list type="number">
///   <item><b>MessageId estavel.</b> A publicacao reutiliza o <c>Id</c> da linha do
///   outbox como <c>MessageId</c>. Se o processo cair entre publicar e marcar como
///   processado, a reentrega chega com o <i>mesmo</i> id e o consumidor idempotente a
///   descarta. Sem isso, um id novo a cada tentativa faria a deduplicacao inutil.</item>
///   <item><b>Limite de tentativas.</b> Uma mensagem que falha sempre (payload
///   corrompido, tipo removido) e aposentada apos
///   <see cref="MaxAttempts"/> tentativas, em vez de bloquear o lote para sempre.</item>
///   <item><b>Falha isolada por mensagem.</b> O try/catch fica dentro do laco: um
///   evento problematico nao impede a publicacao dos demais do mesmo lote.</item>
///   <item><b>Encerramento limpo.</b> O <see cref="PeriodicTimer"/> e o tratamento de
///   <see cref="OperationCanceledException"/> evitam o erro barulhento no log toda vez
///   que o pod recebe SIGTERM.</item>
/// </list>
/// <para>
/// <b>Limitacao conhecida (e proposital para o escopo do projeto):</b> com varias
/// replicas do mesmo servico, todas leem o mesmo lote e podem publicar em duplicidade.
/// Como os consumidores sao idempotentes, o resultado final continua correto. A solucao
/// definitiva seria <c>SELECT ... FOR UPDATE SKIP LOCKED</c> ou eleicao de lider.
/// </para>
/// </remarks>
/// <typeparam name="TDbContext">Contexto do EF Core dono da tabela de outbox.</typeparam>
/// <param name="serviceProvider">Provedor usado para abrir um escopo por ciclo.</param>
/// <param name="logger">Logger do servico.</param>
public sealed class OutboxPublisherBackgroundService<TDbContext>(
    IServiceProvider serviceProvider,
    ILogger<OutboxPublisherBackgroundService<TDbContext>> logger) : BackgroundService
    where TDbContext : DbContext
{
    /// <summary>
    /// Intervalo entre ciclos de publicacao.
    /// </summary>
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Quantidade maxima de mensagens processadas por ciclo.
    /// </summary>
    /// <remarks>
    /// Lote pequeno mantem a transacao curta e o uso de memoria previsivel; o proximo
    /// ciclo vem logo em seguida se houver acumulo.
    /// </remarks>
    private const int BatchSize = 20;

    /// <summary>
    /// Tentativas antes de aposentar uma mensagem envenenada.
    /// </summary>
    private const int MaxAttempts = 5;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Publicador de outbox iniciado para {DbContext}.", typeof(TDbContext).Name);

        using var timer = new PeriodicTimer(PollingInterval);

        try
        {
            // Publica uma vez antes de esperar: no start do pod pode haver backlog do
            // ciclo anterior, e nao ha razao para deixa-lo parado por 5 segundos.
            do
            {
                try
                {
                    await PublishPendingMessagesAsync(stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // Falha de infraestrutura (banco fora, RabbitMQ fora): loga e espera
                    // o proximo ciclo. Deixar a excecao subir mataria o BackgroundService
                    // e o servico pararia de publicar ate o proximo deploy.
                    logger.LogError(exception, "Ciclo do outbox de {DbContext} falhou; nova tentativa no proximo intervalo.", typeof(TDbContext).Name);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Encerramento normal da aplicacao (SIGTERM). Nao e erro.
        }

        logger.LogInformation("Publicador de outbox encerrado para {DbContext}.", typeof(TDbContext).Name);
    }

    private async Task PublishPendingMessagesAsync(CancellationToken cancellationToken)
    {
        // BackgroundService e singleton; DbContext e scoped. Sem este escopo proprio o
        // container lancaria erro de "captive dependency" — e, pior, um DbContext unico
        // acumularia o change tracker por toda a vida do processo.
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(message => message.ProcessedOnUtc == null && message.AttemptCount < MaxAttempts)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            message.AttemptCount++;

            try
            {
                var messageType = Type.GetType(message.Type, throwOnError: true)!;
                var payload = JsonSerializer.Deserialize(message.Payload, messageType)
                              ?? throw new InvalidOperationException($"Payload do outbox {message.Id} desserializou como nulo.");

                await publishEndpoint.Publish(payload, messageType, context =>
                {
                    // Chave da idempotencia ponta a ponta: mesmo evento, mesmo MessageId,
                    // por mais vezes que este bloco execute.
                    context.MessageId = message.Id;
                }, cancellationToken);

                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.Error = exception.Message;

                if (message.AttemptCount >= MaxAttempts)
                {
                    logger.LogError(
                        exception,
                        "Mensagem de outbox {MessageId} ({MessageType}) aposentada apos {AttemptCount} tentativas.",
                        message.Id,
                        message.Type,
                        message.AttemptCount);
                }
                else
                {
                    logger.LogWarning(
                        exception,
                        "Falha ao publicar a mensagem de outbox {MessageId} (tentativa {AttemptCount}/{MaxAttempts}).",
                        message.Id,
                        message.AttemptCount,
                        MaxAttempts);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
