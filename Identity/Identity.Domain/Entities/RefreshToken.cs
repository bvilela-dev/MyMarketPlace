namespace Identity.Domain.Entities;

/// <summary>
/// Represents a refresh token issued to a user.
/// </summary>
public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    /// <summary>
    /// Initializes a new refresh token.
    /// </summary>
    /// <param name="id">The refresh token identifier.</param>
    /// <param name="userId">The owning user identifier.</param>
    /// <param name="token">The opaque token value.</param>
    /// <param name="expiresAt">The UTC expiration timestamp.</param>
    public RefreshToken(Guid id, Guid userId, string token, DateTime expiresAt)
    {
        Id = id;
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Gets the refresh token identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the owning user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the opaque token value.
    /// </summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the UTC expiration timestamp.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the token was revoked.
    /// </summary>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// Revokes the refresh token.
    /// </summary>
    public void Revoke() => IsRevoked = true;
}