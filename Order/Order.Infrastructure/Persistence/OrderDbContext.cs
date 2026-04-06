using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Order.Application.Abstractions;
using Order.Domain.Entities;

namespace Order.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for order data.
/// </summary>
/// <param name="options">The DbContext options.</param>
public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options), IOrderDbContext
{
    /// <summary>
    /// Gets the orders set.
    /// </summary>
    public DbSet<Order.Domain.Entities.Order> Orders => Set<Order.Domain.Entities.Order>();

    /// <summary>
    /// Gets the outbox messages set.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}