using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Order.Infrastructure.Persistence;

/// <summary>
/// Fabrica de <see cref="OrderDbContext"/> para as ferramentas de linha de comando.
/// </summary>
/// <remarks>
/// Usada somente por <c>dotnet ef</c>. A connection string fixa aponta para o ambiente
/// local e nao tem efeito em runtime.
/// </remarks>
public sealed class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    /// <inheritdoc />
    public OrderDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrderDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5434;Database=orderdb;Username=postgres;Password=postgres");
        return new OrderDbContext(optionsBuilder.Options);
    }
}
