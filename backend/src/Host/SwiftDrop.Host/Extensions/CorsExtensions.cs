namespace SwiftDrop.Host.Extensions;

public static class CorsExtensions
{
    public const string FrontendPolicy = "Frontend";

    private static readonly string[] DevelopmentOrigins = ["http://localhost:5173", "http://127.0.0.1:5173"];

    /// Browsers block a page on one origin (the React app on :5173) from calling an API on another
    /// (this backend on :5017) unless the API allows it. Only the origins listed under
    /// "Cors:AllowedOrigins" are allowed, never "*". In Development, both localhost and 127.0.0.1 work.
    ///
    /// Credentials are allowed so the browser can send cookies: the HttpOnly refresh-token cookie
    /// (/api/v1/auth/refresh) and the SignalR real-time connection both need this.
    ///
    /// Keep this the ONLY CORS policy: registering another policy named "Frontend" replaces this one.
    public static IServiceCollection AddFrontendCors(
        this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (environment.IsDevelopment())
            origins = [.. origins.Union(DevelopmentOrigins)];

        services.AddCors(options => options.AddPolicy(FrontendPolicy, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        return services;
    }
}
