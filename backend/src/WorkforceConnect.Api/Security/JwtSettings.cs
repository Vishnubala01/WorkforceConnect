namespace WorkforceConnect.Api.Security;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>Signing key, at least 32 characters. Supply through user secrets or Jwt__Key; never commit a real one.</summary>
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "WorkforceConnect";

    public string Audience { get; set; } = "WorkforceConnect.Client";

    public int ExpiryMinutes { get; set; } = 60;
}
