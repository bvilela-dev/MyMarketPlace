namespace Marketplace.Contracts.Events;

public sealed record OrderCreatedEvent(
    Guid EventId,
    Guid OrderId,
    Guid UserId,
    decimal Total,
    string Currency,
    AddressSnapshotDto Address,
    IReadOnlyCollection<OrderItemDto> Items,
    DateTime CreatedAtUtc);