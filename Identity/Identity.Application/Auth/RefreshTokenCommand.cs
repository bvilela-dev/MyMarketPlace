using MediatR;

namespace Identity.Application.Auth;

/// <summary>
/// Represents the request to refresh an expired access token.
/// </summary>
/// <param name="RefreshToken">The refresh token issued previously.</param>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;