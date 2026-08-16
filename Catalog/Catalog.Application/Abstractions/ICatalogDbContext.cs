using Catalog.Domain.Entities;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Abstractions;

/// <summary>
/// Contrato de persistencia usado pelos casos de uso do Catalog.
/// </summary>
public interface ICatalogDbContext
{
    /// <summary>
    /// Produtos do catalogo.
    /// </summary>
    DbSet<Product> Products { get; }

    /// <summary>
    /// Eventos de integracao pendentes de publicacao (outbox).
    /// </summary>
    DbSet<OutboxMessage> OutboxMessages { get; }

    /// <summary>
    /// Confirma as alteracoes pendentes numa unica transacao.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Quantidade de registros afetados.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
