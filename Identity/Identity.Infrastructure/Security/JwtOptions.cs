namespace Identity.Infrastructure.Security;

/// <summary>
/// Represents JWT configuration values for the Identity service.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets or sets the token issuer.
    /// </summary>
    public string Issuer { get; set; } = "marketplace";

    /// <summary>
    /// Gets or sets the token audience.
    /// </summary>
    public string Audience { get; set; } = "marketplace-clients";

    /// <summary>
    /// Gets or sets the symmetric signing secret.
    /// </summary>
    public string Secret { get; set; } = "super-secret-key-change-me-please-123456789";

    /// <summary>
    /// Gets or sets the access token lifetime in minutes.
    /// </summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    /// <summary>
    /// Gets or sets the refresh token lifetime in days.
    /// </summary>
    public int RefreshTokenLifetimeDays { get; set; } = 30;
}