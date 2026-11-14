using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Merchants.Data.Persistence;
using SwiftDrop.Merchants.Data.Repositories;
using SwiftDrop.SharedKernel.Persistence;

namespace SwiftDrop.Merchants.Data;

public static class MerchantsDataExtensions
{
    public static IServiceCollection AddMerchantsData(this IServiceCollection services, IConfiguration config)
    {
        services.AddModuleDbContext<MerchantsDbContext>(config, "Merchants");
        services.AddScoped<IMerchantRepository, MerchantRepository>();
        return services;
    }

    public static async Task InitializeMerchantsDataAsync(this IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MerchantsDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}

