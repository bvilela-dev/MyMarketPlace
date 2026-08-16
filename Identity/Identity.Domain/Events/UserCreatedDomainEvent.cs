using Marketplace.SharedKernel.Abstractions;

namespace Identity.Domain.Events;

/// <summary>
/// Evento de dominio disparado quando um usuario e criado.
/// </summary>
/// <remarks>
/// Circula apenas dentro do Identity. O <c>UserCreatedEvent</c> (em
/// <c>Marketplace.Contracts</c>) e a versao publica, que trafega pelo RabbitMQ ate o
/// Notification. Manter os dois separados permite evoluir o modelo interno sem quebrar
/// o contrato com os outros servicos.
/// </remarks>
/// <param name="UserId">Identificador do usuario criado.</param>
/// <param name="Name">Nome de exibicao.</param>
/// <param name="Email">E-mail normalizado.</param>
/// <param name="CreatedAtUtc">Momento (UTC) do cadastro.</param>
public sealed record UserCreatedDomainEvent(Guid UserId, string Name, string Email, DateTime CreatedAtUtc) : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
