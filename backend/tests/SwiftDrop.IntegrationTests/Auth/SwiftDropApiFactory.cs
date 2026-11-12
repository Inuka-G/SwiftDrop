using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SwiftDrop.Dispatch.Business.Services;
using SwiftDrop.SharedKernel.Messaging;

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
        Environment.SetEnvironmentVariable("ConnectionStrings__Parcels", $"Data Source={Path.Combine(folder, "parcels.db")}");
        Environment.SetEnvironmentVariable("Parcels__ProofStoragePath", Path.Combine(folder, "proofs"));
        Environment.SetEnvironmentVariable("Parcels__Outbox__PollingIntervalMs", "100");
    }

    /// Captures what the Parcels outbox publishes, standing in for RabbitMQ.
    public RecordingEventPublisher Events { get; } = new();

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
            services.RemoveAll<IIntegrationEventPublisher>();
            services.AddSingleton<IIntegrationEventPublisher>(Events);
        });
    }
}

public sealed class RecordingEventPublisher : IIntegrationEventPublisher
{
    public ConcurrentQueue<(Guid EventId, string RoutingKey, string Payload)> Published { get; } = new();

    public Task PublishAsync(Guid eventId, string routingKey, string payloadJson, CancellationToken ct)
    {
        Published.Enqueue((eventId, routingKey, payloadJson));
        return Task.CompletedTask;
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
