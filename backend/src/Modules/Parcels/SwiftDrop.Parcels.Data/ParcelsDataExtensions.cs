using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Persistence;
using SwiftDrop.Parcels.Data.Repositories;
using SwiftDrop.Parcels.Data.Storage;
using SwiftDrop.SharedKernel.Persistence;

namespace SwiftDrop.Parcels.Data;

public static class ParcelsDataExtensions
{
    public static IServiceCollection AddParcelsData(this IServiceCollection services, IConfiguration config)
    {
        // Registers SQLite (dev) or Postgres (prod) using the shared helper
        services.AddModuleDbContext<ParcelsDbContext>(config, "Parcels");
        services.AddScoped<IParcelRepository, ParcelRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // Proof photos live under "uploads/", which is the persistent volume in Docker
        // (/app/uploads) and a git-ignored folder in dev.
        var proofPath = config["Parcels:ProofStoragePath"] ?? Path.Combine("uploads", "proofs");
        services.AddSingleton<IProofPhotoStorage>(new LocalProofPhotoStorage(Path.GetFullPath(proofPath)));
        return services;
    }

    /// <summary>
    /// Creates the Parcels tables if they are missing. EnsureCreated cannot be used:
    /// in Docker every module shares one Postgres database, and EnsureCreated does
    /// nothing once any module's tables exist. Instead we check for our own table.
    /// </summary>
    public static async Task InitializeParcelsDataAsync(this IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
        var creator = db.GetService<IRelationalDatabaseCreator>();

        if (!await creator.ExistsAsync())
            await creator.CreateAsync();

        if (!await ParcelsTablesExistAsync(db))
        {
            if (db.Database.IsNpgsql())
                await creator.CreateTablesAsync();
            else
                await CreateMissingSqliteTablesAsync(db);
        }
        if (!await ParcelsTableExistsAsync(db))
            await creator.CreateTablesAsync();   // also creates the "parcels" schema on Postgres
        else
            await ParcelOwnershipUpgrade.ApplyAsync(db);
    }

    private static async Task CreateMissingSqliteTablesAsync(ParcelsDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "parcels" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_parcels" PRIMARY KEY,
                "CustomerId" TEXT NOT NULL,
                "TrackingNumber" TEXT NOT NULL,
                "ZoneId" TEXT NOT NULL,
                "PickupAddress" TEXT NOT NULL,
                "PickupContactName" TEXT NOT NULL,
                "PickupContactPhone" TEXT NOT NULL,
                "DropoffAddress" TEXT NOT NULL,
                "RecipientName" TEXT NOT NULL,
                "RecipientPhone" TEXT NOT NULL,
                "WeightKg" TEXT NOT NULL,
                "SizeCategory" TEXT NOT NULL,
                "EstimatedFee" TEXT NOT NULL,
                "FinalFee" TEXT NULL,
                "PaymentMethod" TEXT NOT NULL,
                "PaymentStatus" TEXT NOT NULL,
                "CurrentStatus" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NOT NULL,
                "Version" INTEGER NOT NULL,
                "CourierId" TEXT NULL,
                "AssignedAtUtc" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "status_history" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_status_history" PRIMARY KEY,
                "ParcelId" TEXT NOT NULL,
                "FromStatus" TEXT NULL,
                "ToStatus" TEXT NOT NULL,
                "Note" TEXT NULL,
                "ChangedByUserId" TEXT NOT NULL,
                "ChangedByRole" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_status_history_parcels" FOREIGN KEY ("ParcelId") REFERENCES "parcels" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "delivery_proofs" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_delivery_proofs" PRIMARY KEY,
                "ParcelId" TEXT NOT NULL,
                "CourierId" TEXT NOT NULL,
                "RecipientName" TEXT NULL,
                "Note" TEXT NULL,
                "PhotoKey" TEXT NULL,
                "PhotoContentType" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_delivery_proofs_parcels" FOREIGN KEY ("ParcelId") REFERENCES "parcels" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "failure_reports" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_failure_reports" PRIMARY KEY,
                "ParcelId" TEXT NOT NULL,
                "CourierId" TEXT NOT NULL,
                "Reason" TEXT NOT NULL,
                "Note" TEXT NULL,
                "PhotoKey" TEXT NULL,
                "PhotoContentType" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_failure_reports_parcels" FOREIGN KEY ("ParcelId") REFERENCES "parcels" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "outbox_messages" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_outbox_messages" PRIMARY KEY,
                "RoutingKey" TEXT NOT NULL,
                "Payload" TEXT NOT NULL,
                "ProcessedAtUtc" TEXT NULL,
                "Attempts" INTEGER NOT NULL,
                "LastError" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_status_history_Persisted" ON "status_history" ("ParcelId", "CreatedAtUtc");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_delivery_proofs_Persisted" ON "delivery_proofs" ("ParcelId");
            CREATE INDEX IF NOT EXISTS "IX_failure_reports_Persisted" ON "failure_reports" ("ParcelId");
            CREATE INDEX IF NOT EXISTS "IX_outbox_messages_Persisted" ON "outbox_messages" ("ProcessedAtUtc", "CreatedAtUtc");
            """);
    }

    private static async Task<bool> ParcelsTablesExistAsync(ParcelsDbContext db)
    {
        var entityTypes = new[]
        {
            typeof(Parcel),
            typeof(ParcelStatusHistory),
            typeof(DeliveryProof),
            typeof(FailureReport),
            typeof(OutboxMessage)
        };

        foreach (var entityType in entityTypes)
        {
            var entity = db.Model.FindEntityType(entityType)!;
            var table = entity.GetTableName()!;
            var count = db.Database.IsNpgsql()
                ? await db.Database.SqlQuery<int>(
                    $"SELECT COUNT(*)::int AS \"Value\" FROM information_schema.tables WHERE table_schema = {entity.GetSchema() ?? "public"} AND table_name = {table}")
                    .SingleAsync()
                : await db.Database.SqlQuery<int>(
                    $"SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = {table}")
                    .SingleAsync();

            if (count == 0) return false;
        }

        return true;
    }
}
