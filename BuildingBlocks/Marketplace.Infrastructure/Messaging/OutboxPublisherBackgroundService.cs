using System.Text.Json;
using MassTransit;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Marketplace.Infrastructure.Messaging;

/// <summary>
/// Periodically publishes pending outbox messages for a DbContext.
/// </summary>
/// <typeparam name="TDbContext">The DbContext type that owns the outbox table.</typeparam>
/// <param name="serviceProvider">The service provider used to create publishing scopes.</param>
/// <param name="logger">The logger used to record publishing errors.</param>
public sealed class OutboxPublisherBackgroundService<TDbContext>(
    IServiceProvider serviceProvider,
    ILogger<OutboxPublisherBackgroundService<TDbContext>> logger) : BackgroundService
    where TDbContext : DbContext
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingMessagesAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish outbox messages for {DbContext}.", typeof(TDbContext).Name);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task PublishPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var messageType = Type.GetType(message.Type, throwOnError: true)!;
                var payload = JsonSerializer.Deserialize(message.Payload, messageType)!;

                await publishEndpoint.Publish(payload, messageType, cancellationToken);

                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception exception)
            {
                message.Error = exception.Message;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}