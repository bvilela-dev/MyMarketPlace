namespace Identity.Application.Auth;

/// <summary>
/// Represents the authentication payload returned by auth endpoints.
/// </summary>
/// <param name="UserId">The authenticated user identifier.</param>
/// <param name="Name">The user display name.</param>
/// <param name="Email">The user email address.</param>
/// <param name="AccessToken">The JWT access token.</param>
/// <param name="AccessTokenExpiresAtUtc">The UTC expiration timestamp of the access token.</param>
/// <param name="RefreshToken">The opaque refresh token.</param>
/// <param name="RefreshTokenExpiresAtUtc">The UTC expiration timestamp of the refresh token.</param>
public sealed record AuthResponse(Guid UserId, string Name, string Email, string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);