namespace Cart.Domain.Entities;

/// <summary>
/// Represents the complete shopping cart for a user.
/// </summary>
public sealed class ShoppingCart
{
    /// <summary>
    /// Gets the owning user identifier.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Gets the items stored in the cart.
    /// </summary>
    public IReadOnlyCollection<CartItem> Items { get; init; } = [];
}

/// <summary>
/// Represents a cart line item.
/// </summary>
/// <param name="ProductId">The product identifier.</param>
/// <param name="Name">The product name.</param>
/// <param name="UnitPrice">The product unit price.</param>
/// <param name="Quantity">The item quantity.</param>
public sealed record CartItem(Guid ProductId, string Name, decimal UnitPrice, int Quantity);