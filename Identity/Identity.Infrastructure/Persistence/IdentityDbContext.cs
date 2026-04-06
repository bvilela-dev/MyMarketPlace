using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for Identity data.
/// </summary>
/// <param name="options">The DbContext options.</param>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options), IIdentityDbContextAdapter
{
    /// <summary>
    /// Gets the users set.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets the refresh tokens set.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Gets the addresses set.
    /// </summary>
    public DbSet<Address> Addresses => Set<Address>();

    /// <summary>
    /// Gets the outbox messages set.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}