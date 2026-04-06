namespace Marketplace.Contracts.Events;

public sealed record PaymentApprovedEvent(Guid EventId, Guid OrderId, Guid UserId, decimal Total, DateTime ApprovedAtUtc);