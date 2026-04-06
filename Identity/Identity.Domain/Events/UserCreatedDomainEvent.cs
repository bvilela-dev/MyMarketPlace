using Marketplace.SharedKernel.Abstractions;

namespace Identity.Domain.Events;

public sealed record UserCreatedDomainEvent(Guid UserId, string Name, string Email, DateTime CreatedAtUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}