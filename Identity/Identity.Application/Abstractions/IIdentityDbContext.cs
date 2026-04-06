using Identity.Domain.Entities;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Abstractions;

/// <summary>
/// Defines the persistence contract used by the Identity application layer.
/// </summary>
public interface IIdentityDbContext
{
    /// <summary>
    /// Gets the users set.
    /// </summary>
    DbSet<User> Users { get; }

    /// <summary>
    /// Gets the refresh tokens set.
    /// </summary>
    DbSet<RefreshToken> RefreshTokens { get; }

    /// <summary>
    /// Gets the addresses set.
    /// </summary>
    DbSet<Address> Addresses { get; }

    /// <summary>
    /// Gets the outbox messages set.
    /// </summary>
    DbSet<OutboxMessage> OutboxMessages { get; }

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The number of persisted state entries.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}