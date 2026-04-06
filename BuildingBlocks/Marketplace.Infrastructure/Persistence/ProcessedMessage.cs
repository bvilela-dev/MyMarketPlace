namespace Marketplace.Infrastructure.Persistence;

/// <summary>
/// Represents a message already processed by a given consumer.
/// </summary>
public sealed class ProcessedMessage
{
    /// <summary>
    /// Gets or sets the processed message identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the consumer name.
    /// </summary>
    public string ConsumerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC processing timestamp.
    /// </summary>
    public DateTime ProcessedOnUtc { get; set; }
}