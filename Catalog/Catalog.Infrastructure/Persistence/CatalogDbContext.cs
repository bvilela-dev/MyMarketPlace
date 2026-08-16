using Catalog.Application.Abstractions;
using Catalog.Domain.Entities;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

/// <summary>
/// Contexto do EF Core do banco do Catalog.
/// </summary>
/// <param name="options">Opcoes de configuracao do contexto.</param>
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options), ICatalogDbContext
{
    /// <summary>
    /// Produtos do catalogo.
    /// </summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>
    /// Eventos de integracao pendentes (outbox).
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        modelBuilder.ApplyOutboxConfiguration();
        base.OnModelCreating(modelBuilder);
    }
}
