using Identity.Domain.Entities;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Abstractions;

/// <summary>
/// Contrato de persistencia usado pelos casos de uso do Identity.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por que uma interface e nao o <c>DbContext</c> concreto?</b> Para que os handlers
/// dependam de uma abstracao e possam ser testados com um contexto em memoria, sem
/// Postgres. E a mesma ideia do padrao Repository, mas sem a camada extra: os
/// <c>DbSet</c> ja sao repositorios (<c>IQueryable</c>) e <c>SaveChangesAsync</c> ja e
/// o Unit of Work.
/// </para>
/// <para>
/// <b>Compromisso assumido:</b> a interface expoe tipos do EF Core, entao a camada de
/// aplicacao nao esta 100% isolada do ORM. E uma troca consciente — a alternativa
/// (repositorios manuais para tudo) custa muito codigo repetitivo para proteger de uma
/// troca de ORM que quase nunca acontece.
/// </para>
/// </remarks>
public interface IIdentityDbContext
{
    /// <summary>
    /// Usuarios cadastrados.
    /// </summary>
    DbSet<User> Users { get; }

    /// <summary>
    /// Refresh tokens emitidos.
    /// </summary>
    DbSet<RefreshToken> RefreshTokens { get; }

    /// <summary>
    /// Enderecos dos usuarios.
    /// </summary>
    DbSet<Address> Addresses { get; }

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
