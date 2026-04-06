using System.Text.Json;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Messaging;

/// <summary>
/// Persists integration events into the transactional outbox.
/// </summary>
/// <typeparam name="TDbContext">The DbContext type that owns the outbox table.</typeparam>
/// <param name="dbContext">The DbContext used to persist outbox messages.</param>
public sealed class IntegrationEventOutboxWriter<TDbContext>(TDbContext dbContext)
    where TDbContext : DbContext
{
    /// <summary>
    /// Adds an integration event to the outbox.
    /// </summary>
    /// <typeparam name="TMessage">The integration event type.</typeparam>
    /// <param name="message">The integration event instance.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>A task representing the asynchronous add operation.</returns>
    public Task AddAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var entity = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(TMessage).AssemblyQualifiedName ?? typeof(TMessage).FullName ?? typeof(TMessage).Name,
            Payload = JsonSerializer.Serialize(message),
            OccurredOnUtc = DateTime.UtcNow
        };

        return dbContext.Set<OutboxMessage>().AddAsync(entity, cancellationToken).AsTask();
    }
}