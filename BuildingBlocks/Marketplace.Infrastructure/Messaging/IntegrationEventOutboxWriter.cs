using System.Text.Json;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Messaging;

/// <summary>
/// Grava eventos de integracao no outbox transacional.
/// </summary>
/// <remarks>
/// <para>
/// Primeira metade do padrao Outbox. O ponto essencial e o que este tipo
/// <b>nao</b> faz: ele nao chama <c>SaveChanges</c>. A linha do outbox e apenas
/// <i>rastreada</i> pelo <c>DbContext</c>, e sera gravada no mesmo
/// <c>SaveChangesAsync</c> que persiste a mudanca de negocio — logo, na mesma
/// transacao.
/// </para>
/// <para>
/// Uso tipico dentro de um handler:
/// </para>
/// <code>
/// dbContext.Orders.Add(order);
/// await outbox.AddAsync(new OrderCreatedEvent(...), ct);
/// await dbContext.SaveChangesAsync(ct);   // pedido + evento: tudo ou nada
/// </code>
/// </remarks>
/// <typeparam name="TDbContext">Contexto do EF Core dono da tabela de outbox.</typeparam>
/// <param name="dbContext">Contexto usado para rastrear a nova linha de outbox.</param>
public sealed class IntegrationEventOutboxWriter<TDbContext>(TDbContext dbContext)
    where TDbContext : DbContext
{
    /// <summary>
    /// Enfileira um evento de integracao no outbox.
    /// </summary>
    /// <typeparam name="TMessage">Tipo do evento de integracao.</typeparam>
    /// <param name="message">Instancia do evento.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Task da operacao assincrona.</returns>
    public Task AddAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var entity = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            // AssemblyQualifiedName permite reconstruir o tipo exato na publicacao.
            Type = typeof(TMessage).AssemblyQualifiedName ?? typeof(TMessage).FullName ?? typeof(TMessage).Name,
            Payload = JsonSerializer.Serialize(message),
            OccurredOnUtc = DateTime.UtcNow
        };

        return dbContext.Set<OutboxMessage>().AddAsync(entity, cancellationToken).AsTask();
    }
}
