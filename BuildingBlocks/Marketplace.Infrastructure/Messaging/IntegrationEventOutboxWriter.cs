using System.Text.Json;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Messaging;

public sealed class IntegrationEventOutboxWriter<TDbContext>(TDbContext dbContext)
    where TDbContext : DbContext
{
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