using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures persistence mapping for <see cref="Address"/>.
/// </summary>
public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");
        builder.HasKey(address => address.Id);
        builder.Property(address => address.Street).HasMaxLength(256).IsRequired();
        builder.Property(address => address.Number).HasMaxLength(32).IsRequired();
        builder.Property(address => address.City).HasMaxLength(120).IsRequired();
        builder.Property(address => address.State).HasMaxLength(120).IsRequired();
        builder.Property(address => address.ZipCode).HasMaxLength(32).IsRequired();
        builder.Property(address => address.Country).HasMaxLength(120).IsRequired();
    }
}