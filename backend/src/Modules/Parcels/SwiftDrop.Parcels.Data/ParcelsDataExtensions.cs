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

        if (!await ParcelsTableExistsAsync(db))
            await creator.CreateTablesAsync();   // also creates the "parcels" schema on Postgres
        else
            await ParcelOwnershipUpgrade.ApplyAsync(db);
    }

    private static async Task<bool> ParcelsTableExistsAsync(ParcelsDbContext db)
    {
        var entity = db.Model.FindEntityType(typeof(Parcel))!;
        var table = entity.GetTableName()!;

        var count = db.Database.IsNpgsql()
            ? await db.Database.SqlQuery<int>(
                $"SELECT COUNT(*)::int AS \"Value\" FROM information_schema.tables WHERE table_schema = {entity.GetSchema() ?? "public"} AND table_name = {table}")
                .SingleAsync()
            : await db.Database.SqlQuery<int>(
                $"SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = {table}")
                .SingleAsync();

        return count > 0;
    }
}
