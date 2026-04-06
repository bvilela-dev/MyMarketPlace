namespace Marketplace.Infrastructure.Persistence;

/// <summary>
/// Represents an integration event persisted in the transactional outbox.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the serialized .NET type name.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized payload.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime OccurredOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC processing timestamp.
    /// </summary>
    public DateTime? ProcessedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the last processing error, when present.
    /// </summary>
    public string? Error { get; set; }
}