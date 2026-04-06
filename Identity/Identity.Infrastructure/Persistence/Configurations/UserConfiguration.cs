using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Name).HasMaxLength(120).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.HasMany(user => user.Addresses).WithOne().HasForeignKey(address => address.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(user => user.RefreshTokens).WithOne().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(User.Addresses))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.RefreshTokens))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}