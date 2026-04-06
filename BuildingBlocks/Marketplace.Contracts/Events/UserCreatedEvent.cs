namespace Marketplace.Contracts.Events;

/// <summary>
/// Notifies consumers that a new user has been created.
/// </summary>
/// <param name="EventId">The unique identifier of the integration event.</param>
/// <param name="UserId">The unique identifier of the created user.</param>
/// <param name="Name">The user display name.</param>
/// <param name="Email">The user email address.</param>
/// <param name="CreatedAtUtc">The UTC timestamp of user creation.</param>
public sealed record UserCreatedEvent(Guid EventId, Guid UserId, string Name, string Email, DateTime CreatedAtUtc);