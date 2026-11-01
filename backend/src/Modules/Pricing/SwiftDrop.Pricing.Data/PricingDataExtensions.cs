using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Pricing.Data.Persistence;
using SwiftDrop.Pricing.Data.Repositories;
using SwiftDrop.SharedKernel.Persistence;

namespace SwiftDrop.Pricing.Data;

public static class PricingDataExtensions
{
    public static IServiceCollection AddPricingData(this IServiceCollection services, IConfiguration config)
    {
        services.AddModuleDbContext<PricingDbContext>(config, "Pricing");
        services.AddScoped<IZoneRepository, ZoneRepository>();
        return services;
    }

    public static async Task InitializePricingDataAsync(this IServiceProvider sp, bool seed)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
        await db.Database.EnsureCreatedAsync();   // dev only, see "Migrations" below
        if (seed) await PricingDbSeeder.SeedAsync(db);
    }
}