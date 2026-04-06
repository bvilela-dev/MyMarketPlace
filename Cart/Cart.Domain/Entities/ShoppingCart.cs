namespace Cart.Domain.Entities;

public sealed class ShoppingCart
{
    public Guid UserId { get; init; }

    public IReadOnlyCollection<CartItem> Items { get; init; } = [];
}

public sealed record CartItem(Guid ProductId, string Name, decimal UnitPrice, int Quantity);