namespace Marketplace.Infrastructure.Persistence;

public sealed class ProcessedMessage
{
    public Guid Id { get; set; }

    public string ConsumerName { get; set; } = string.Empty;

    public DateTime ProcessedOnUtc { get; set; }
}