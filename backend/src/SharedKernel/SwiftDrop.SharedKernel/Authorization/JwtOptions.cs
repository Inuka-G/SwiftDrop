namespace SwiftDrop.SharedKernel.Authorization;

/// Bound from the "Jwt" configuration section.
/// Key is a secret: it is never committed. Set it with the Jwt__Key environment variable.
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; set; } = "SwiftDrop";
    public string Audience { get; set; } = "SwiftDrop.Client";
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
