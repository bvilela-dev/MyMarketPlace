namespace Marketplace.Contracts.Events;

public sealed record UserCreatedEvent(Guid EventId, Guid UserId, string Name, string Email, DateTime CreatedAtUtc);