using MediatR;

namespace Identity.Application.Auth;

/// <summary>
/// Represents the request to register a new user.
/// </summary>
/// <param name="Name">The user display name.</param>
/// <param name="Email">The unique user email.</param>
/// <param name="Password">The plain-text password to hash and store.</param>
public sealed record RegisterUserCommand(string Name, string Email, string Password) : IRequest<AuthResponse>;