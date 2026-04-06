namespace Marketplace.Contracts.Events;

public sealed record PaymentFailedEvent(Guid EventId, Guid OrderId, Guid UserId, string Reason, DateTime FailedAtUtc);