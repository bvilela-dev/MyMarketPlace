using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Order.Infrastructure.Persistence;

/// <summary>
/// Creates <see cref="OrderDbContext"/> instances for design-time tooling.
/// </summary>
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