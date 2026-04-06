namespace Identity.Application.Models;

/// <summary>
/// Represents a user profile exposed by the Identity service.
/// </summary>
/// <param name="Id">The user identifier.</param>
/// <param name="Name">The user display name.</param>
/// <param name="Email">The user email address.</param>
/// <param name="CreatedAt">The user creation timestamp.</param>
/// <param name="Addresses">The list of user addresses.</param>
public sealed record UserDto(Guid Id, string Name, string Email, DateTime CreatedAt, IReadOnlyCollection<AddressDto> Addresses);