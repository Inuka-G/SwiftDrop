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
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE dispatch.parcel_assignments
                    ADD COLUMN IF NOT EXISTS "CurrentStatus" varchar(32) NOT NULL DEFAULT 'Created';
                ALTER TABLE dispatch.parcel_assignments
                    ADD COLUMN IF NOT EXISTS "StatusOccurredAtUtc" bigint NOT NULL DEFAULT 0;
                CREATE TABLE IF NOT EXISTS dispatch.outbox_messages (
                    "EventId" uuid PRIMARY KEY,
                    "RoutingKey" varchar(160) NOT NULL,
                    "Payload" text NOT NULL,
                    "CreatedAt" bigint NOT NULL,
                    "ProcessedAt" bigint NULL,
                    "Attempts" integer NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS "IX_outbox_messages_ProcessedAt_CreatedAt"
                    ON dispatch.outbox_messages ("ProcessedAt", "CreatedAt");
                """);
        else
        {
            await EnsureSqliteColumnAsync(db, "CurrentStatus");
            await EnsureSqliteColumnAsync(db, "StatusOccurredAtUtc");
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "outbox_messages" (
                    "EventId" TEXT NOT NULL PRIMARY KEY,
                    "RoutingKey" TEXT NOT NULL,
                    "Payload" TEXT NOT NULL,
                    "CreatedAt" INTEGER NOT NULL,
                    "ProcessedAt" INTEGER NULL,
                    "Attempts" INTEGER NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS "IX_outbox_messages_ProcessedAt_CreatedAt"
                    ON "outbox_messages" ("ProcessedAt", "CreatedAt");
                """);
        }
    }

    private static async Task EnsureSqliteColumnAsync(DbContext db, string column)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            var connection = db.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA table_info('parcel_assignments');";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase)) return;
            await reader.CloseAsync();
            var sql = column switch
            {
                "CurrentStatus" => "ALTER TABLE parcel_assignments ADD COLUMN CurrentStatus TEXT NOT NULL DEFAULT 'Created';",
                "StatusOccurredAtUtc" => "ALTER TABLE parcel_assignments ADD COLUMN StatusOccurredAtUtc INTEGER NOT NULL DEFAULT 0;",
                _ => throw new InvalidOperationException("Unsupported Dispatch schema column.")
            };
            await db.Database.ExecuteSqlRawAsync(sql);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
