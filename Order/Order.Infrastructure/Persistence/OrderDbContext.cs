using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Order.Application.Abstractions;

namespace Order.Infrastructure.Persistence;

/// <summary>
/// Contexto do EF Core do banco do Order.
/// </summary>
/// <param name="options">Opcoes de configuracao do contexto.</param>
public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options), IOrderDbContext
{
    /// <summary>
    /// Pedidos.
    /// </summary>
    public DbSet<Domain.Entities.Order> Orders => Set<Domain.Entities.Order>();

    /// <summary>
    /// Eventos de integracao pendentes (outbox).
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);
        modelBuilder.ApplyOutboxConfiguration();
        base.OnModelCreating(modelBuilder);
    }
}
