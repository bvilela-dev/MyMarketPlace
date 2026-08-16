using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Contexto do EF Core do banco do Inventory.
/// </summary>
/// <param name="options">Opcoes de configuracao do contexto.</param>
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Saldos de estoque por produto.
    /// </summary>
    public DbSet<StockItem> StockItems => Set<StockItem>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
