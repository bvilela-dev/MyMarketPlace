using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures persistence mapping for <see cref="StockItem"/>.
/// </summary>
public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => item.ProductId).IsUnique();
        builder.Property(item => item.QuantityAvailable).IsRequired();
    }
}