using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace SwiftDrop.IntegrationTests.Auth;

/// Starts the real SwiftDrop app in memory, using throwaway SQLite files.
public sealed class SwiftDropApiFactory : WebApplicationFactory<Program>
{
    public SwiftDropApiFactory()
    {
        var folder = Directory.CreateTempSubdirectory("swiftdrop-tests-").FullName;
        Environment.SetEnvironmentVariable("Database__Provider", "Sqlite");
        Environment.SetEnvironmentVariable("ConnectionStrings__Identity", $"Data Source={Path.Combine(folder, "identity.db")}");
        Environment.SetEnvironmentVariable("ConnectionStrings__Pricing", $"Data Source={Path.Combine(folder, "pricing.db")}");
        Environment.SetEnvironmentVariable("ConnectionStrings__Dispatch", $"Data Source={Path.Combine(folder, "dispatch.db")}");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");   // seeds the four demo logins
        builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ThrowingTestController).Assembly));
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<SwiftDropApiFactory>
{
    public const string Name = "SwiftDrop API";
}
