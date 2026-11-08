using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Parcels.Data.Persistence;
using SwiftDrop.Parcels.Data.Repositories;
using SwiftDrop.SharedKernel.Persistence;

namespace SwiftDrop.Parcels.Data;

public static class ParcelsDataExtensions
{
    public static IServiceCollection AddParcelsData(this IServiceCollection services, IConfiguration config)
    {
        // Registers SQLite (dev) or Postgres (prod) using the shared helper
        services.AddModuleDbContext<ParcelsDbContext>(config, "Parcels");
        services.AddScoped<IParcelRepository, ParcelRepository>();
        return services;
    }

    public static async Task InitializeParcelsDataAsync(this IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}