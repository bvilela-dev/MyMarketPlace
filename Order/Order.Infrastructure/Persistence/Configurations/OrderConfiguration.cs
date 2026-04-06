using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;

namespace Order.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures persistence mapping for <see cref="Order.Domain.Entities.Order"/>.
/// </summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order.Domain.Entities.Order>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Order.Domain.Entities.Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.UserId).IsRequired();
        builder.Property(order => order.Total).HasPrecision(18, 2).IsRequired();
        builder.Property(order => order.Status).HasMaxLength(64).IsRequired();
        builder.Property(order => order.CreatedAtUtc).IsRequired();

        builder.OwnsOne(order => order.AddressSnapshot, owned =>
        {
            owned.Property(snapshot => snapshot.Street).HasColumnName("street").HasMaxLength(256);
            owned.Property(snapshot => snapshot.Number).HasColumnName("number").HasMaxLength(32);
            owned.Property(snapshot => snapshot.City).HasColumnName("city").HasMaxLength(120);
            owned.Property(snapshot => snapshot.State).HasColumnName("state").HasMaxLength(120);
            owned.Property(snapshot => snapshot.ZipCode).HasColumnName("zip_code").HasMaxLength(32);
            owned.Property(snapshot => snapshot.Country).HasColumnName("country").HasMaxLength(120);
        });

        builder.Metadata.FindNavigation(nameof(Order.Domain.Entities.Order.Items))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(order => order.Items).WithOne().OnDelete(DeleteBehavior.Cascade);
    }
}