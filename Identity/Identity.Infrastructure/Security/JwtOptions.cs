namespace Identity.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "marketplace";

    public string Audience { get; set; } = "marketplace-clients";

    public string Secret { get; set; } = "super-secret-key-change-me-please-123456789";

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    public int RefreshTokenLifetimeDays { get; set; } = 30;
}