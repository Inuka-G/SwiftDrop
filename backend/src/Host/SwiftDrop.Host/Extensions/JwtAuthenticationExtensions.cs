using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Host.Extensions;

public static class JwtAuthenticationExtensions
{
    /// Teaches the app to check the JWT on each incoming request and read its role.
    public static WebApplicationBuilder AddJwtAuthentication(this WebApplicationBuilder builder)
    {
        EnsureSigningKey(builder);

        var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;   // keep the claim names exactly as "sub" and "role"
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "name",
                    RoleClaimType = "role",
                };
            });
        builder.Services.AddAuthorization();

        return builder;
    }

    /// The key is a secret, so it is never stored in the repository. Real deployments must
    /// set Jwt__Key. In Development a random key is made at startup, so the app runs with no
    /// setup; tokens simply stop working whenever the app restarts, and you log in again.
    private static void EnsureSigningKey(WebApplicationBuilder builder)
    {
        var configKey = $"{JwtOptions.SectionName}:{nameof(JwtOptions.Key)}";
        var key = builder.Configuration[configKey];

        if (string.IsNullOrEmpty(key))
        {
            if (!builder.Environment.IsDevelopment())
                throw new InvalidOperationException("Jwt__Key is not set. Set it as an environment variable.");

            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [configKey] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
            });
        }
        else if (Encoding.UTF8.GetByteCount(key) < JwtOptions.MinimumKeyBytes)
        {
            throw new InvalidOperationException($"Jwt__Key must be at least {JwtOptions.MinimumKeyBytes} bytes long.");
        }
    }
}
