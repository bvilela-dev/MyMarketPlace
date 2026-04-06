namespace Marketplace.Contracts.Events;

/// <summary>
/// Notifies downstream services that a new order has been created.
/// </summary>
/// <param name="EventId">The unique identifier of the integration event.</param>
/// <param name="OrderId">The order identifier.</param>
/// <param name="UserId">The user who created the order.</param>
/// <param name="Total">The total amount of the order.</param>
/// <param name="Currency">The currency code used by the order total.</param>
/// <param name="Address">The immutable address snapshot associated with the order.</param>
/// <param name="Items">The ordered items.</param>
/// <param name="CreatedAtUtc">The UTC timestamp of order creation.</param>
public sealed record OrderCreatedEvent(
    Guid EventId,
    Guid OrderId,
    Guid UserId,
    decimal Total,
    string Currency,
    AddressSnapshotDto Address,
    IReadOnlyCollection<OrderItemDto> Items,
    DateTime CreatedAtUtc);