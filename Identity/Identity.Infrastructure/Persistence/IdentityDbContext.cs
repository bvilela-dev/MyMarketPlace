using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Contexto do EF Core do banco do Identity.
/// </summary>
/// <remarks>
/// Cada microsservico tem o <b>seu proprio banco</b> (database-per-service). Nenhum
/// servico le a tabela do outro: a troca de dados acontece por API ou por evento. E o
/// que permite ao Identity mudar seu esquema sem quebrar Order, Cart ou Catalog.
/// </remarks>
/// <param name="options">Opcoes de configuracao do contexto.</param>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options), IIdentityDbContext
{
    /// <summary>
    /// Usuarios cadastrados.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Refresh tokens emitidos.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Enderecos dos usuarios.
    /// </summary>
    public DbSet<Address> Addresses => Set<Address>();

    /// <summary>
    /// Eventos de integracao pendentes (outbox).
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Varre o assembly em busca de IEntityTypeConfiguration<T>. Assim cada entidade
        // tem seu mapeamento num arquivo proprio, em vez de um OnModelCreating gigante.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // O mapeamento do outbox vive nos building blocks e precisa ser aplicado a mao:
        // ApplyConfigurationsFromAssembly so enxerga o assembly deste servico.
        modelBuilder.ApplyOutboxConfiguration();

        base.OnModelCreating(modelBuilder);
    }
}
