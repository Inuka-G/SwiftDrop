using Microsoft.OpenApi;

namespace SwiftDrop.Host.Extensions;

public static class SwaggerExtensions
{
    /// Adds Swagger with an "Authorize" button, where you paste a JWT to call protected endpoints.
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        const string scheme = "bearer";

        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition(scheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = scheme,
                BearerFormat = "JWT",
                Description = "Paste the accessToken returned by POST /api/v1/auth/login.",
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(scheme, document)] = [],
            });
        });

        return services;
    }
}
