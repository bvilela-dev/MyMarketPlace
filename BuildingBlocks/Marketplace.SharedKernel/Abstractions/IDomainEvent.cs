namespace Marketplace.SharedKernel.Abstractions;

/// <summary>
/// Defines the contract for a domain event emitted inside an aggregate boundary.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    DateTime OccurredOnUtc { get; }
}