using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Parcels.Data;
using SwiftDrop.Parcels.Data.Persistence;

namespace SwiftDrop.Parcels.UnitTests;

/// In Docker all modules share one database. These tests make sure Parcels still
/// creates its tables when another module created its own tables first.
public sealed class ParcelsDataInitializationTests : IDisposable
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");
    private readonly ServiceProvider services;

    public ParcelsDataInitializationTests()
    {
        connection.Open();
        services = new ServiceCollection()
            .AddDbContext<ParcelsDbContext>(o => o.UseSqlite(connection))
            .BuildServiceProvider();
    }

    [Fact]
    public async Task Creates_parcels_tables_when_another_module_already_has_tables()
    {
        Execute("CREATE TABLE users (Id TEXT PRIMARY KEY)");   // e.g. Identity started first

        await services.InitializeParcelsDataAsync();

        TableNames().Should().Contain(["parcels", "status_history", "delivery_proofs", "failure_reports", "outbox_messages"]);
    }

    [Fact]
    public async Task Running_twice_keeps_existing_data()
    {
        await services.InitializeParcelsDataAsync();
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
            db.Parcels.Add(new Data.Entities.Parcel(Guid.NewGuid(), "SD-INIT-0001", Guid.NewGuid(),
                "12 Galle Road, Colombo 03", "Sender Perera", "+94771234567",
                "45 Temple Street, Kandy", "Nimal Silva", "+94779876543", 1m, "Small", 500m, "Card"));
            await db.SaveChangesAsync();
        }

        await services.InitializeParcelsDataAsync();   // e.g. the app restarts

        using var check = services.CreateScope();
        (await check.ServiceProvider.GetRequiredService<ParcelsDbContext>().Parcels.CountAsync()).Should().Be(1);
    }

    private void Execute(string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private List<string> TableNames()
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
        using var reader = command.ExecuteReader();
        var names = new List<string>();
        while (reader.Read()) names.Add(reader.GetString(0));
        return names;
    }

    public void Dispose()
    {
        services.Dispose();
        connection.Dispose();
    }
}
