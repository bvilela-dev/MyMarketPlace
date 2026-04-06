using Marketplace.SharedKernel.Abstractions;
using Order.Domain.ValueObjects;

namespace Order.Domain.Entities;

public sealed class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

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

    public Guid UserId { get; private set; }

    public decimal Total { get; private set; }

    public string Status { get; private set; } = OrderStatus.PendingPayment;

    public DateTime CreatedAtUtc { get; private set; }

    public AddressSnapshot AddressSnapshot { get; private set; } = null!;

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
}

public static class OrderStatus
{
    public const string PendingPayment = "PendingPayment";
}