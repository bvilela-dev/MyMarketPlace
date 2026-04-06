using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Creates <see cref="InventoryDbContext"/> instances for design-time tooling.
/// </summary>
public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    /// <inheritdoc />
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5435;Database=inventorydb;Username=postgres;Password=postgres");
        return new InventoryDbContext(optionsBuilder.Options);
    }
}