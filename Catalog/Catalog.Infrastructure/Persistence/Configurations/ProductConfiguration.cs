using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures persistence mapping for <see cref="Product"/>.
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Name).HasMaxLength(120).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(1024).IsRequired();
        builder.Property(product => product.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(product => product.AvailableQuantity).IsRequired();
    }
}