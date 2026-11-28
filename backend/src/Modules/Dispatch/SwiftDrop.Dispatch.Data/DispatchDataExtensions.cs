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
                ALTER TABLE dispatch.parcel_assignments
                    ADD COLUMN IF NOT EXISTS "WeightKg" numeric(8,2) NULL;
                ALTER TABLE dispatch.parcel_assignments
                    ADD COLUMN IF NOT EXISTS "SizeCategory" varchar(50) NULL;
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
            await EnsureSqliteColumnAsync(db, "WeightKg");
            await EnsureSqliteColumnAsync(db, "SizeCategory");
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

        // Tracking numbers are unique in Parcels. Remove stale duplicate rows
        // before enforcing the same invariant on the Dispatch projection.
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlRawAsync("""
                WITH ranked AS (
                    SELECT ctid, ROW_NUMBER() OVER (
                        PARTITION BY "TrackingNumber"
                        ORDER BY CASE WHEN "CourierId" IS NULL THEN 1 ELSE 0 END,
                                 "AssignedAt" DESC NULLS LAST, "CreatedAt" DESC, "ParcelId") AS row_number
                    FROM dispatch.parcel_assignments
                )
                DELETE FROM dispatch.parcel_assignments AS assignment
                USING ranked
                WHERE assignment.ctid = ranked.ctid AND ranked.row_number > 1;
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_parcel_assignments_TrackingNumber"
                    ON dispatch.parcel_assignments ("TrackingNumber");
                """);
        else
            await db.Database.ExecuteSqlRawAsync("""
                DELETE FROM parcel_assignments
                WHERE rowid IN (
                    SELECT rowid FROM (
                        SELECT rowid, ROW_NUMBER() OVER (
                            PARTITION BY TrackingNumber
                            ORDER BY CASE WHEN CourierId IS NULL THEN 1 ELSE 0 END,
                                     AssignedAt DESC, CreatedAt DESC, ParcelId) AS row_number
                        FROM parcel_assignments
                    ) AS ranked
                    WHERE row_number > 1
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_parcel_assignments_TrackingNumber"
                    ON parcel_assignments (TrackingNumber);
                """);
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
                "WeightKg" => "ALTER TABLE parcel_assignments ADD COLUMN WeightKg TEXT NULL;",
                "SizeCategory" => "ALTER TABLE parcel_assignments ADD COLUMN SizeCategory TEXT NULL;",
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
