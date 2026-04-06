namespace Marketplace.Contracts.Events;

public sealed record StockReservedEvent(Guid EventId, Guid OrderId, Guid UserId, DateTime ReservedAtUtc);