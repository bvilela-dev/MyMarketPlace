using Marketplace.SharedKernel.Abstractions;

namespace Catalog.Domain.Entities;

public sealed class Product : AggregateRoot
{
    private Product()
    {
    }

    public Product(Guid id, string name, string description, decimal price, int availableQuantity)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        AvailableQuantity = availableQuantity;
    }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public int AvailableQuantity { get; private set; }
}