namespace Marketplace.Infrastructure.Persistence;

/// <summary>
/// Evento de integracao gravado na tabela de outbox, aguardando publicacao.
/// </summary>
/// <remarks>
/// <para>
/// <b>O problema que o outbox resolve (dual write).</b> Criar um pedido exige duas
/// acoes: gravar no Postgres e publicar <c>OrderCreated</c> no RabbitMQ. Sao dois
/// sistemas distintos, sem transacao distribuida entre eles. Se o processo cair no
/// meio:
/// </para>
/// <list type="bullet">
///   <item>gravou e nao publicou → pedido pago nunca vira cobranca nem separacao no
///   estoque: some silenciosamente;</item>
///   <item>publicou e nao gravou → estoque reservado para um pedido que nao existe.</item>
/// </list>
/// <para>
/// <b>A solucao.</b> Em vez de publicar na hora, o evento e inserido nesta tabela
/// <i>dentro da mesma transacao</i> que grava o pedido. Ou os dois commitam, ou nenhum
/// dos dois — o banco garante a atomicidade. Um processo separado
/// (<see cref="Messaging.OutboxPublisherBackgroundService{TDbContext}"/>) le as linhas
/// pendentes e publica no barramento.
/// </para>
/// <para>
/// <b>Contrapartida.</b> A entrega passa a ser <i>at-least-once</i>: se a publicacao
/// funciona mas a marcacao de "processado" falha, o evento e reenviado. Por isso todo
/// consumidor precisa ser idempotente (ver <c>RedisMessageDeduplicator</c>).
/// </para>
/// </remarks>
public sealed class OutboxMessage
{
    /// <summary>
    /// Identificador da mensagem. Tambem e usado como <c>MessageId</c> na publicacao,
    /// permitindo que o consumidor detecte reentrega.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Nome do tipo .NET do evento serializado.
    /// </summary>
    /// <remarks>
    /// Guarda o <c>AssemblyQualifiedName</c> para que o publicador consiga reconstruir
    /// o objeto original e o MassTransit publique no exchange correto. E por isso que
    /// os eventos vivem no projeto <c>Marketplace.Contracts</c>: se o tipo fosse
    /// renomeado ou movido de assembly, as linhas ja gravadas nao seriam mais
    /// desserializaveis.
    /// </remarks>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Corpo do evento serializado em JSON (coluna <c>jsonb</c> no Postgres).
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Momento (UTC) em que o evento foi produzido. Define a ordem de publicacao.
    /// </summary>
    public DateTime OccurredOnUtc { get; set; }

    /// <summary>
    /// Momento (UTC) da publicacao bem-sucedida; <see langword="null"/> enquanto pendente.
    /// </summary>
    public DateTime? ProcessedOnUtc { get; set; }

    /// <summary>
    /// Numero de tentativas de publicacao ja realizadas.
    /// </summary>
    /// <remarks>
    /// Sem este contador, uma mensagem "envenenada" (payload que sempre falha ao
    /// desserializar) seria retentada para sempre, a cada ciclo, ocupando o lote e
    /// travando a fila de eventos validos atras dela.
    /// </remarks>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Mensagem do ultimo erro de publicacao, quando houver.
    /// </summary>
    public string? Error { get; set; }
}
