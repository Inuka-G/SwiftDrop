using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Identity.Data.Persistence;
using SwiftDrop.Identity.Data.Repositories;
using SwiftDrop.SharedKernel.Persistence;

namespace SwiftDrop.Identity.Data;

public static class IdentityDataExtensions
{
    public static IServiceCollection AddIdentityData(this IServiceCollection services, IConfiguration config)
    {
        services.AddModuleDbContext<IdentityDbContext>(config, "Identity");
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }

    public static async Task InitializeIdentityDataAsync(this IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.EnsureCreatedAsync();   // dev only: replace with migrations when moving to Postgres
    }
}
