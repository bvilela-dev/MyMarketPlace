using Marketplace.SharedKernel.Abstractions;

namespace Catalog.Domain.Entities;

/// <summary>
/// Represents a catalog product aggregate.
/// </summary>
public sealed class Product : AggregateRoot
{
    private Product()
    {
    }

    /// <summary>
    /// Initializes a new catalog product.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="name">The product name.</param>
    /// <param name="description">The product description.</param>
    /// <param name="price">The product price.</param>
    /// <param name="availableQuantity">The available stock quantity.</param>
    public Product(Guid id, string name, string description, decimal price, int availableQuantity)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        AvailableQuantity = availableQuantity;
    }

    /// <summary>
    /// Gets the product name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the product description.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the current product price.
    /// </summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// Gets the available inventory quantity.
    /// </summary>
    public int AvailableQuantity { get; private set; }
}