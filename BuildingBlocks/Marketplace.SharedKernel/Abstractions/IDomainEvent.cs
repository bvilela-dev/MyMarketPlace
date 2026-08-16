namespace Marketplace.SharedKernel.Abstractions;

/// <summary>
/// Contrato de um evento de dominio: um fato de negocio que ja aconteceu.
/// </summary>
/// <remarks>
/// <para>
/// Eventos de dominio sao sempre nomeados no <b>passado</b> (<c>UserCreated</c>,
/// <c>PaymentApproved</c>) porque descrevem algo consumado — nao um pedido para agir.
/// </para>
/// <para>
/// Nao confundir com <b>evento de integracao</b> (pasta <c>Marketplace.Contracts</c>):
/// <list type="bullet">
///   <item><b>Dominio</b>: circula apenas dentro do processo do servico dono do agregado.
///   Pode mudar livremente, pois nao e um contrato publico.</item>
///   <item><b>Integracao</b>: trafega pelo RabbitMQ ate outros servicos. Alterar sua
///   forma quebra terceiros, entao versionamento importa.</item>
/// </list>
/// </para>
/// </remarks>
public interface IDomainEvent
{
    /// <summary>
    /// Identificador unico desta ocorrencia do evento.
    /// </summary>
    /// <remarks>
    /// E a chave usada por consumidores idempotentes para reconhecer entregas repetidas
    /// (ver <c>RedisMessageDeduplicator</c>).
    /// </remarks>
    Guid EventId { get; }

    /// <summary>
    /// Momento (UTC) em que o fato ocorreu.
    /// </summary>
    /// <remarks>
    /// Sempre UTC: containers e nos de Kubernetes podem estar em fusos diferentes, e
    /// horario local tornaria a ordenacao dos eventos nao confiavel.
    /// </remarks>
    DateTime OccurredOnUtc { get; }
}
