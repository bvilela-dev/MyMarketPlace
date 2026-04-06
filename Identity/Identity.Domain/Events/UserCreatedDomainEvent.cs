using Marketplace.SharedKernel.Abstractions;

namespace Identity.Domain.Events;

/// <summary>
/// Represents the domain event raised when a user is created.
/// </summary>
/// <param name="UserId">The created user identifier.</param>
/// <param name="Name">The created user name.</param>
/// <param name="Email">The created user email.</param>
/// <param name="CreatedAtUtc">The UTC creation timestamp.</param>
public sealed record UserCreatedDomainEvent(Guid UserId, string Name, string Email, DateTime CreatedAtUtc) : IDomainEvent
{
    /// <summary>
    /// Gets the unique event identifier.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}