using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Infrastructure.Persistence;

/// <summary>
/// Fabrica de <see cref="CatalogDbContext"/> para as ferramentas de linha de comando.
/// </summary>
/// <remarks>
/// Usada apenas em tempo de projeto, por <c>dotnet ef migrations add</c> e
/// <c>dotnet ef database update</c>. Sem ela, o CLI tentaria subir o host inteiro da API
/// (com RabbitMQ, Redis e afins) so para descobrir o modelo do EF.
/// <para>
/// A connection string fixa aqui aponta para o ambiente local e <b>nao</b> e usada em
/// runtime — a real vem da configuracao/variaveis de ambiente.
/// </para>
/// </remarks>
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
