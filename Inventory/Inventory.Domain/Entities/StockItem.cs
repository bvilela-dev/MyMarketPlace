namespace Inventory.Domain.Entities;

/// <summary>
/// Represents a stock entry for a product.
/// </summary>
public sealed class StockItem
{
    /// <summary>
    /// Gets or sets the stock item identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product identifier.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the available quantity.
    /// </summary>
    public int QuantityAvailable { get; set; }
}