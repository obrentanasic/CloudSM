namespace SmartMetering.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "SmartMetering";

    public string Audience { get; set; } = "SmartMetering";

    public string Secret { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 480;
}
