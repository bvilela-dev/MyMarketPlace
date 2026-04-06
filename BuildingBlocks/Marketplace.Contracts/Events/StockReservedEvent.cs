namespace Marketplace.Contracts.Events;

/// <summary>
/// Notifies consumers that stock was reserved for an order.
/// </summary>
/// <param name="EventId">The unique identifier of the integration event.</param>
/// <param name="OrderId">The order identifier.</param>
/// <param name="UserId">The user who owns the order.</param>
/// <param name="ReservedAtUtc">The UTC timestamp of stock reservation.</param>
public sealed record StockReservedEvent(Guid EventId, Guid OrderId, Guid UserId, DateTime ReservedAtUtc);