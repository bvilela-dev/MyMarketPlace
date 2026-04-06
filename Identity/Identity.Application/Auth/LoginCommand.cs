using MediatR;

namespace Identity.Application.Auth;

/// <summary>
/// Represents the request to authenticate a user.
/// </summary>
/// <param name="Email">The user email.</param>
/// <param name="Password">The plain-text password.</param>
public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;