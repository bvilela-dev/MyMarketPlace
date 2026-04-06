using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;

namespace Order.Application.Abstractions;

/// <summary>
/// Defines persistence operations required by the Order application layer.
/// </summary>
public interface IOrderDbContext
{
    /// <summary>
    /// Gets the orders set.
    /// </summary>
    DbSet<Order.Domain.Entities.Order> Orders { get; }

    /// <summary>
    /// Gets the outbox messages set.
    /// </summary>
    DbSet<OutboxMessage> OutboxMessages { get; }

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The number of affected records.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}