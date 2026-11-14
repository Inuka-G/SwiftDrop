namespace SwiftDrop.Host.Extensions;

public static class CorsExtensions
{
    public const string FrontendPolicy = "Frontend";

    /// Browsers block a page on one origin (the React app on :5173) from calling an API on another
    /// (this backend on :5017) unless the API allows it. Only the origins listed under
    /// "Cors:AllowedOrigins" are allowed, never "*".
    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration config)
    {
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(FrontendPolicy, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }
}
