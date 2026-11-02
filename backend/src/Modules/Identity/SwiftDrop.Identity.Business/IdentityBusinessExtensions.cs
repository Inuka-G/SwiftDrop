using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Identity.Business.Services;
using SwiftDrop.Identity.Data;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.Identity.Data.Repositories;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Identity.Business;

public static class IdentityBusinessExtensions
{
    public static IServiceCollection AddIdentityBusiness(this IServiceCollection services, IConfiguration config)
    {
        services.AddIdentityData(config);
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();   // PBKDF2 with a random salt per user
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    public static async Task InitializeIdentityAsync(this IServiceProvider sp, bool seedDemoUsers)
    {
        await sp.InitializeIdentityDataAsync();
        if (!seedDemoUsers) return;

        using var scope = sp.CreateScope();
        await DemoUserSeeder.SeedAsync(
            scope.ServiceProvider.GetRequiredService<IUserRepository>(),
            scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>(),
            CancellationToken.None);
    }
}
