using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures persistence mapping for <see cref="RefreshToken"/>.
/// </summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Token).HasMaxLength(512).IsRequired();
        builder.HasIndex(token => token.Token).IsUnique();
        builder.Property(token => token.ExpiresAt).IsRequired();
        builder.Property(token => token.IsRevoked).IsRequired();
    }
}