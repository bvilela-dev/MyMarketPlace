using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Fabrica de <see cref="InventoryDbContext"/> para as ferramentas de linha de comando.
/// </summary>
/// <remarks>
/// Usada somente por <c>dotnet ef</c>. A connection string fixa aponta para o ambiente
/// local e nao tem efeito em runtime.
/// </remarks>
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
