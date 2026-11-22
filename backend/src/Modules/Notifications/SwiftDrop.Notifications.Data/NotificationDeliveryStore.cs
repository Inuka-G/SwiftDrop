using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.SharedKernel.Persistence;

namespace SwiftDrop.Notifications.Data;

public static class NotificationsDataExtensions
{
    public static IServiceCollection AddNotificationsData(this IServiceCollection services, IConfiguration config)
    {
        services.AddModuleDbContext<NotificationsDbContext>(config, "Notifications");
        services.AddScoped(sp => new NotificationDeliveryStore(sp.GetRequiredService<NotificationsDbContext>()));
        return services;
    }
}

internal sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options);

public sealed class NotificationDeliveryStore
{
    private readonly NotificationsDbContext db;

    internal NotificationDeliveryStore(NotificationsDbContext db) => this.db = db;

    public async Task InitializeAsync(CancellationToken ct)
    {
        if (db.Database.IsNpgsql())
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlRawAsync("""
                SELECT pg_advisory_xact_lock(286540611, 2);
                CREATE SCHEMA IF NOT EXISTS notifications;
                CREATE TABLE IF NOT EXISTS notifications.email_deliveries (event_id uuid PRIMARY KEY);
                """, ct);
            await transaction.CommitAsync(ct);
        }
        else
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS email_deliveries (event_id TEXT PRIMARY KEY NOT NULL)", ct);
        }
    }

    public async Task SendOnceAsync(Guid eventId, Func<CancellationToken, Task> send, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Reserve the event in the same transaction that records a successful send.
        // A second delivery waits on the unique key, and a failed send rolls back the reservation.
        var inserted = db.Database.IsNpgsql()
            ? await db.Database.ExecuteSqlAsync($"""
                INSERT INTO notifications.email_deliveries (event_id) VALUES ({eventId})
                ON CONFLICT (event_id) DO NOTHING
                """, ct)
            : await db.Database.ExecuteSqlAsync($"""
                INSERT INTO email_deliveries (event_id) VALUES ({eventId})
                ON CONFLICT (event_id) DO NOTHING
                """, ct);
        if (inserted == 1)
            await send(ct);
        await transaction.CommitAsync(ct);
    }
}
