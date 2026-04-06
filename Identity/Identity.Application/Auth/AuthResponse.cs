namespace Identity.Application.Auth;

public sealed record AuthResponse(Guid UserId, string Name, string Email, string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);