using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Dispatch.Data.Persistence;
using SwiftDrop.Dispatch.Data.Repositories;
using SwiftDrop.SharedKernel.Persistence;

namespace SwiftDrop.Dispatch.Data;

public static class DispatchDataExtensions
{
    public static IServiceCollection AddDispatchData(this IServiceCollection services, IConfiguration config)
    {
        services.AddModuleDbContext<DispatchDbContext>(config, "Dispatch");
        services.AddScoped<IDispatchRepository, DispatchRepository>();
        return services;
    }

    public static async Task InitializeDispatchDataAsync(this IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DispatchDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!db.Database.IsNpgsql())
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "courier_profiles" (
                    "CourierId" TEXT NOT NULL CONSTRAINT "PK_courier_profiles" PRIMARY KEY,
                    "ZoneId" TEXT NULL,
                    "IsAvailable" INTEGER NOT NULL,
                    "VehicleType" TEXT NULL,
                    "LicensePlate" TEXT NULL,
                    "Version" INTEGER NOT NULL
                );
                CREATE INDEX IF NOT EXISTS "IX_courier_profiles_ZoneId_IsAvailable"
                    ON "courier_profiles" ("ZoneId", "IsAvailable");
                """);
        }
    }
}
