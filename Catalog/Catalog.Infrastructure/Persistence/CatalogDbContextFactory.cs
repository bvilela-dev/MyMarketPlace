using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Infrastructure.Persistence;

/// <summary>
/// Creates <see cref="CatalogDbContext"/> instances for design-time tooling.
/// </summary>
public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    /// <inheritdoc />
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=catalogdb;Username=postgres;Password=postgres");
        return new CatalogDbContext(optionsBuilder.Options);
    }
}