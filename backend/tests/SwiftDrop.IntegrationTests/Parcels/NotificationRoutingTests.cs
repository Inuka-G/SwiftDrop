using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SwiftDrop.Notifications.Business;
using SwiftDrop.Parcels.Business;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Persistence;
using SwiftDrop.SharedKernel.Messaging;
using Testcontainers.RabbitMq;

namespace SwiftDrop.IntegrationTests.Parcels;

public sealed class NotificationRoutingTests
{
    [RabbitMqFact]
    public async Task Outbox_retries_an_unroutable_status_then_delivers_it_to_notifications()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var ct = timeout.Token;
        await using var rabbit = new RabbitMqBuilder("rabbitmq:3.13-management-alpine").Build();
        await rabbit.StartAsync(ct);

        using var folder = new TemporaryDatabase();
        var evt = new ParcelStatusChangedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(),
            "SD-ROUTING-TEST", null, Guid.NewGuid(), "PickedUp", "InTransit", null, DateTime.UtcNow, Guid.NewGuid());
        var received = new TaskCompletionSource<ParcelStatusChangedIntegrationEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using (var outboxOnly = BuildHost(withNotifications: false))
        {
            await outboxOnly.Services.InitializeParcelsAsync();
            using (var scope = outboxOnly.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
                db.OutboxMessages.Add(new OutboxMessage(evt.EventId, ParcelEventRoutingKeys.StatusChanged,
                    JsonSerializer.Serialize(evt, JsonSerializerOptions.Web), evt.OccurredAtUtc));
                await db.SaveChangesAsync(ct);
            }

            await outboxOnly.StartAsync(ct);
            var failed = await WaitForOutboxAsync(outboxOnly, m => m.Attempts > 0, ct);
            Assert.Null(failed.ProcessedAtUtc);
            await outboxOnly.StopAsync(ct);
        }

        using var withConsumer = BuildHost(withNotifications: true);
        await withConsumer.StartAsync(ct);
        Assert.Equal(evt, await received.Task.WaitAsync(ct));
        var published = await WaitForOutboxAsync(withConsumer, m => m.ProcessedAtUtc.HasValue, ct);
        Assert.Null(published.LastError);
        await withConsumer.StopAsync(ct);

        IHost BuildHost(bool withNotifications)
        {
            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                EnvironmentName = Environments.Production
            });
            builder.Configuration["Database:Provider"] = "Sqlite";
            builder.Configuration["ConnectionStrings:Parcels"] = $"Data Source={folder.Path};Pooling=False";
            builder.Configuration["Parcels:Outbox:PollingIntervalMs"] = "250";
            builder.Configuration["Parcels:Outbox:MaxAttempts"] = "200";
            builder.Services.AddParcelsBusiness(builder.Configuration);
            builder.Services.AddRabbitMq(builder.Configuration);
            var uri = new Uri(rabbit.GetConnectionString());
            var credentials = uri.UserInfo.Split(':', 2);
            builder.Services.PostConfigure<RabbitMqOptions>(options =>
            {
                options.Host = uri.Host;
                options.Port = uri.Port;
                options.Username = Uri.UnescapeDataString(credentials[0]);
                options.Password = Uri.UnescapeDataString(credentials[1]);
                options.RequiredOutboxRoutes = [ParcelEventRoutingKeys.StatusChanged];
            });
            if (withNotifications)
            {
                builder.Services.AddNotificationsBusiness();
                builder.Services.AddSingleton<IIntegrationEventHandler<ParcelStatusChangedIntegrationEvent>>(
                    new RecordingHandler(received));
            }
            return builder.Build();
        }
    }

    private static async Task<OutboxMessage> WaitForOutboxAsync(
        IHost host, Func<OutboxMessage, bool> predicate, CancellationToken ct)
    {
        while (true)
        {
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
            var message = await db.OutboxMessages.AsNoTracking().SingleAsync(ct);
            if (predicate(message))
                return message;
            await Task.Delay(100, ct);
        }
    }

    private sealed class RecordingHandler(TaskCompletionSource<ParcelStatusChangedIntegrationEvent> received)
        : IIntegrationEventHandler<ParcelStatusChangedIntegrationEvent>
    {
        public Task HandleAsync(ParcelStatusChangedIntegrationEvent evt, CancellationToken ct)
        {
            received.TrySetResult(evt);
            return Task.CompletedTask;
        }
    }

    private sealed class TemporaryDatabase : IDisposable
    {
        private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("swiftdrop-notification-routing-");
        public string Path => System.IO.Path.Combine(directory.FullName, "parcels.db");
        public void Dispose() => directory.Delete(recursive: true);
    }
}

internal sealed class RabbitMqFactAttribute : FactAttribute
{
    public RabbitMqFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SWIFTDROP_TEST_RABBITMQ") != "1")
            Skip = "Set SWIFTDROP_TEST_RABBITMQ=1 to run this test with Docker.";
    }
}
