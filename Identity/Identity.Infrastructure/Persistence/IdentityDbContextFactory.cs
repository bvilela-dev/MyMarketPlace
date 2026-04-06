using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Creates <see cref="IdentityDbContext"/> instances for design-time tooling.
/// </summary>
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