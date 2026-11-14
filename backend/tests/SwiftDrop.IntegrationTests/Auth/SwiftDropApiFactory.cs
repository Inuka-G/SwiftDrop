using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SwiftDrop.Dispatch.Business.Services;

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
        Environment.SetEnvironmentVariable("ConnectionStrings__Reporting", $"Data Source={Path.Combine(folder, "reporting.db")}");
        Environment.SetEnvironmentVariable("ConnectionStrings__Merchants", $"Data Source={Path.Combine(folder, "merchants.db")}");

        // Keep Google login switched off in tests, even on a computer that has the real secrets.
        // A space is used because an empty value would delete the variable instead of overriding it.
        Environment.SetEnvironmentVariable("Authentication__Google__ClientId", " ");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");   // seeds the four demo logins
        builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(ThrowingTestController).Assembly);
            services.RemoveAll<ICourierEligibilityGateway>();
            services.RemoveAll<IParcelAssignmentGateway>();
            services.RemoveAll<IDispatchEventPublisher>();
            services.RemoveAll<IZoneLookup>();
            services.AddScoped<ICourierEligibilityGateway, TestCourierIdentityGateway>();
            services.AddScoped<IParcelAssignmentGateway, TestParcelAssignmentGateway>();
            services.AddScoped<IDispatchEventPublisher, TestDispatchEventPublisher>();
            services.AddScoped<IZoneLookup, TestZoneLookup>();
        });
    }
}

internal sealed class TestCourierIdentityGateway : ICourierEligibilityGateway
{
    public Task<bool> IsEligibleAsync(Guid courierId, CancellationToken ct) => Task.FromResult(true);
    public Task<bool> IsActiveCourierAsync(Guid userId, CancellationToken ct) => Task.FromResult(true);
    public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(userIds.ToDictionary(id => id, _ => "Test Courier"));
}

internal sealed class TestParcelAssignmentGateway : IParcelAssignmentGateway
{
    public Task<bool> AssignAsync(Guid parcelId, Guid courierId, CancellationToken ct) => Task.FromResult(true);
}

internal sealed class TestDispatchEventPublisher : IDispatchEventPublisher
{
    public Task PublishAsync(SwiftDrop.Dispatch.Contracts.CourierAssignedEvent message, CancellationToken ct) => Task.CompletedTask;
}

internal sealed class TestZoneLookup : IZoneLookup
{
    public Task<bool> ExistsAsync(Guid zoneId, CancellationToken ct) => Task.FromResult(true);
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<SwiftDropApiFactory>
{
    public const string Name = "SwiftDrop API";
}
