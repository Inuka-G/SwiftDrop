using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SwiftDrop.SharedKernel.Persistence;

public static class ModuleDbContextExtensions
{
    /// Registers a module's DbContext. Sqlite in dev (one file per module),
    /// PostgreSQL when Database:Provider = "Postgres".
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services, IConfiguration config, string module)
        where TContext : DbContext
    {
        var provider = config["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<TContext>(options =>
        {
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

            if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = config.GetConnectionString("Postgres")
                    ?? "Host=localhost;Port=5433;Database=swiftdrop;Username=postgres;Password=postgres;Include Error Detail=true;";
                options.UseNpgsql(connectionString,
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", module.ToLowerInvariant()));
            }
            else
            {
                var cs = config.GetConnectionString(module) ?? $"Data Source={module.ToLowerInvariant()}.db";
                options.UseSqlite(cs);
            }
        });

        return services;
    }
}