using Marketplace.SharedKernel.Abstractions;
using Order.Domain.ValueObjects;

namespace Order.Domain.Entities;

/// <summary>
/// Represents an order aggregate.
/// </summary>
public sealed class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    /// <summary>
    /// Initializes a new order aggregate.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="userId">The ordering user identifier.</param>
    /// <param name="addressSnapshot">The immutable shipping address snapshot.</param>
    /// <param name="items">The order items.</param>
    public Order(Guid id, Guid userId, AddressSnapshot addressSnapshot, IReadOnlyCollection<OrderItem> items)
    {
        Id = id;
        UserId = userId;
        AddressSnapshot = addressSnapshot;
        CreatedAtUtc = DateTime.UtcNow;
        Status = OrderStatus.PendingPayment;
        _items.AddRange(items);
        Total = items.Sum(item => item.UnitPrice * item.Quantity);
    }

    /// <summary>
    /// Gets the ordering user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the order total.
    /// </summary>
    public decimal Total { get; private set; }

    /// <summary>
    /// Gets the current order status.
    /// </summary>
    public string Status { get; private set; } = OrderStatus.PendingPayment;

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the immutable address snapshot captured at order creation time.
    /// </summary>
    public AddressSnapshot AddressSnapshot { get; private set; } = null!;

    /// <summary>
    /// Gets the order items.
    /// </summary>
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
}

/// <summary>
/// Contains known order status values.
/// </summary>
public static class OrderStatus
{
    /// <summary>
    /// Indicates that the order is waiting for payment processing.
    /// </summary>
    public const string PendingPayment = "PendingPayment";
}