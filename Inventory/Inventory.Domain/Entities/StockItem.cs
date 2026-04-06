namespace Inventory.Domain.Entities;

public sealed class StockItem
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public int QuantityAvailable { get; set; }
}