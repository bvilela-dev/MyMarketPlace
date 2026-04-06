using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
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