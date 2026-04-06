namespace Order.Domain.Entities;

/// <summary>
/// Represents an item inside an order.
/// </summary>
public sealed class OrderItem
{
    private OrderItem()
    {
    }

    /// <summary>
    /// Initializes a new order item.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="name">The product name.</param>
    /// <param name="unitPrice">The unit price.</param>
    /// <param name="quantity">The quantity.</param>
    public OrderItem(Guid productId, string name, decimal unitPrice, int quantity)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Name = name;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    /// <summary>
    /// Gets the order item identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the product identifier.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Gets the product name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the unit price.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Gets the quantity.
    /// </summary>
    public int Quantity { get; private set; }
}