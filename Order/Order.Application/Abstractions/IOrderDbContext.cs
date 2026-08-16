using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Order.Application.Abstractions;

/// <summary>
/// Contrato de persistencia usado pelos casos de uso e consumidores do Order.
/// </summary>
public interface IOrderDbContext
{
    /// <summary>
    /// Pedidos.
    /// </summary>
    DbSet<Domain.Entities.Order> Orders { get; }

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
