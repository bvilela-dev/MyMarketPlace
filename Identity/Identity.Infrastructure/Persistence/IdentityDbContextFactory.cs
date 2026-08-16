using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Fabrica de <see cref="IdentityDbContext"/> para as ferramentas de linha de comando.
/// </summary>
/// <remarks>
/// Usada apenas em tempo de projeto, por <c>dotnet ef migrations add</c> e
/// <c>dotnet ef database update</c>. Sem ela, o CLI tentaria subir o host inteiro da
/// API — com RabbitMQ, Redis e afins — so para descobrir o modelo do EF Core.
/// <para>
/// A connection string fixa aponta para o ambiente local e <b>nao</b> tem efeito em
/// runtime: a real vem da configuracao/variaveis de ambiente.
/// </para>
/// </remarks>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    /// <inheritdoc />
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=identitydb;Username=postgres;Password=postgres");
        return new IdentityDbContext(optionsBuilder.Options);
    }
}
