using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Order.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures persistence mapping for <see cref="OutboxMessage"/> in Order.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Type).HasMaxLength(512).IsRequired();
        builder.Property(message => message.Payload).HasColumnType("jsonb").IsRequired();
    }
}