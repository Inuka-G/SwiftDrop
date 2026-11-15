namespace SwiftDrop.SharedKernel.Authorization;

/// Bound from the "Authentication:Google" configuration section.
/// ClientId and ClientSecret come from Google Cloud Console and are never committed:
/// set them with "dotnet user-secrets" locally, or as environment variables when deployed.
public sealed class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    /// The authentication scheme that talks to Google.
    public const string Scheme = "Google";

    /// A short-lived cookie that holds the Google result between Google's redirect and our JWT.
    public const string ExternalCookieScheme = "External";

    /// Google redirects the browser here after sign-in. It must be registered in Google Cloud Console.
    public const string CallbackPath = "/signin-google";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// The React page that finishes the sign-in. It receives either ?code=... or ?error=...
    public string FrontendCallbackUrl { get; set; } = "http://localhost:5173/auth/google/callback";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
