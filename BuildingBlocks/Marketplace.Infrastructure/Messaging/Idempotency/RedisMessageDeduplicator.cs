using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Marketplace.Infrastructure.Messaging.Idempotency;

/// <summary>
/// Deduplicacao de mensagens em Redis, usada para tornar os consumidores idempotentes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por que isso e obrigatorio.</b> RabbitMQ (como praticamente todo broker) entrega
/// <i>at-least-once</i>: se o consumidor processa a mensagem mas cai antes de enviar o
/// ACK, o broker reentrega. Sem protecao, o cliente seria cobrado duas vezes e o
/// estoque baixaria em dobro.
/// </para>
/// <para>
/// <b>Como funciona.</b> Cada consumidor tenta gravar uma chave
/// <c>consumer:{nome}:message:{id}</c> com <c>When.NotExists</c> — o <c>SET NX</c> do
/// Redis, que e atomico. Quem consegue gravar, processa; quem encontra a chave ja
/// existente, ignora a mensagem.
/// </para>
/// <para>
/// <b>Por que a chave inclui o nome do consumidor.</b> O mesmo evento
/// <c>PaymentApproved</c> e consumido por Inventory <i>e</i> por Notification. Uma
/// chave apenas por id da mensagem faria o segundo consumidor achar que ja processou o
/// evento — e a notificacao nunca sairia.
/// </para>
/// <para>
/// <b>E por que o nome precisa ser QUALIFICADO (com namespace).</b> Este foi um bug
/// real do projeto, encontrado rodando o fluxo ponta a ponta: Inventory e Notification
/// tem, cada um, uma classe chamada <c>PaymentApprovedConsumer</c>. Usando
/// <c>nameof</c>/<c>GetType().Name</c>, os dois geravam a MESMA chave
/// <c>consumer:PaymentApprovedConsumer:message:{id}</c> — o primeiro a processar
/// marcava, e o segundo descartava a mensagem como "duplicada".
/// </para>
/// <para>
/// Sintoma observado: o estoque era reservado, mas o e-mail de confirmacao de pagamento
/// nunca saia — sem nenhum erro em log. Passando <c>GetType().FullName</c>
/// (<c>Inventory.Application.Consumers.PaymentApprovedConsumer</c> x
/// <c>Notification.Application.Consumers.PaymentApprovedConsumer</c>), as chaves ficam
/// distintas e cada consumidor tem a sua propria marca de processamento.
/// </para>
/// <para>
/// <b>Limitacao honesta.</b> Isto e uma trava otimista, nao uma transacao: se o
/// processo cair <i>depois</i> de marcar e <i>antes</i> de concluir o trabalho, a
/// reentrega sera descartada e o efeito se perde. A versao a prova de falhas grava a
/// marcacao na mesma transacao do banco do proprio consumidor (tabela
/// <c>processed_messages</c>). Redis foi escolhido aqui por ser compartilhado entre
/// servicos que nem sempre tem banco proprio, como o Notification.
/// </para>
/// </remarks>
/// <param name="connectionMultiplexer">Conexao compartilhada com o Redis.</param>
/// <param name="logger">Logger usado para registrar as duplicatas descartadas.</param>
public sealed class RedisMessageDeduplicator(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<RedisMessageDeduplicator> logger)
{
    /// <summary>
    /// Tempo de retencao da marca de processamento.
    /// </summary>
    /// <remarks>
    /// Sete dias cobre com folga qualquer cenario realista de reentrega (fila parada,
    /// replay manual) e evita que o Redis cresca indefinidamente.
    /// </remarks>
    private static readonly TimeSpan Retention = TimeSpan.FromDays(7);

    /// <summary>
    /// Tenta reservar o processamento de uma mensagem por um consumidor.
    /// </summary>
    /// <param name="messageId">Identificador da mensagem.</param>
    /// <param name="consumerName">
    /// Nome <b>qualificado</b> do consumidor — sempre <c>GetType().FullName</c>, nunca
    /// <c>nameof</c>. Ver a nota sobre colisao de nomes na documentacao da classe.
    /// </param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>
    /// <see langword="true"/> quando o processamento pode seguir;
    /// <see langword="false"/> quando a mensagem ja foi processada por este consumidor.
    /// </returns>
    public async Task<bool> TryBeginAsync(Guid messageId, string consumerName, CancellationToken cancellationToken = default)
    {
        // A API do StackExchange.Redis nao recebe CancellationToken; a checagem manual
        // evita iniciar um round-trip quando o host ja esta em processo de desligamento.
        cancellationToken.ThrowIfCancellationRequested();

        var database = connectionMultiplexer.GetDatabase();
        var key = $"consumer:{consumerName}:message:{messageId}";

        // When.NotExists => SET NX: grava somente se a chave ainda nao existir.
        // A operacao e atomica no servidor, entao duas replicas do mesmo consumidor
        // competindo pela mesma mensagem nunca processam as duas.
        var acquired = await database.StringSetAsync(key, DateTime.UtcNow.ToString("O"), Retention, When.NotExists);

        if (!acquired)
        {
            logger.LogInformation(
                "Mensagem {MessageId} ignorada: ja processada por {ConsumerName}.",
                messageId,
                consumerName);
        }

        return acquired;
    }
}
