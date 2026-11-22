using System.Text.Json;
using System.Threading.Channels;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using SwiftDrop.Identity.Contracts;
using SwiftDrop.Merchants.Contracts;
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
    public async Task Outbox_retries_an_unroutable_status_then_sends_one_email_for_duplicate_deliveries()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var ct = timeout.Token;
        await using var rabbit = new RabbitMqBuilder("rabbitmq:3.13-management-alpine").Build();
        await rabbit.StartAsync(ct);
        await using var mailpit = new ContainerBuilder("axllent/mailpit:v1.27.8")
            .WithPortBinding(1025, true)
            .WithPortBinding(8025, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(1025))
            .Build();
        await mailpit.StartAsync(ct);
        using var mailbox = new HttpClient
        {
            BaseAddress = new UriBuilder("http", mailpit.Hostname, mailpit.GetMappedPublicPort(8025)).Uri
        };

        using var folder = new TemporaryDatabase();
        var evt = new ParcelStatusChangedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(),
            "SD-ROUTING-TEST", null, Guid.NewGuid(), "PickedUp", "InTransit", null, DateTime.UtcNow, Guid.NewGuid());
        var received = Channel.CreateUnbounded<ParcelStatusChangedIntegrationEvent>();

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
        await withConsumer.Services.InitializeNotificationsAsync(ct);
        await withConsumer.StartAsync(ct);
        Assert.Equal(evt, await received.Reader.ReadAsync(ct));
        var published = await WaitForOutboxAsync(withConsumer, m => m.ProcessedAtUtc.HasValue, ct);
        Assert.Null(published.LastError);

        await withConsumer.Services.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(
            evt.EventId, ParcelEventRoutingKeys.StatusChanged, published.Payload, ct);
        Assert.Equal(evt, await received.Reader.ReadAsync(ct));
        using var messages = JsonDocument.Parse(await mailbox.GetStringAsync("/api/v1/messages", ct));
        var message = Assert.Single(messages.RootElement.GetProperty("messages").EnumerateArray());
        Assert.Equal("SwiftDrop parcel status update", message.GetProperty("Subject").GetString());
        Assert.Equal("merchant@example.test",
            Assert.Single(message.GetProperty("To").EnumerateArray()).GetProperty("Address").GetString());

        await mailpit.StopAsync(ct);
        var undeliverable = evt with { EventId = Guid.NewGuid() };
        await withConsumer.Services.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(
            undeliverable.EventId, ParcelEventRoutingKeys.StatusChanged,
            JsonSerializer.Serialize(undeliverable, JsonSerializerOptions.Web), ct);
        var broker = withConsumer.Services.GetRequiredService<RabbitMqConnection>();
        var connection = await broker.GetAsync(ct);
        await using (var channel = await connection.CreateChannelAsync(cancellationToken: ct))
        {
            BasicGetResult? rejected;
            while ((rejected = await channel.BasicGetAsync("q.notifications.dlq", autoAck: true, ct)) is null)
                await Task.Delay(100, ct);
            Assert.Equal(undeliverable.EventId.ToString(), rejected.BasicProperties.MessageId);
        }

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
                options.MaxHandlerAttempts = 2;
            });
            if (withNotifications)
            {
                builder.Configuration["ConnectionStrings:Notifications"] =
                    $"Data Source={folder.NotificationsPath};Pooling=False";
                builder.Configuration["Notifications:Email:Enabled"] = "true";
                builder.Configuration["Notifications:Email:Host"] = mailpit.Hostname;
                builder.Configuration["Notifications:Email:Port"] = mailpit.GetMappedPublicPort(1025).ToString();
                builder.Configuration["Notifications:Email:EnableSsl"] = "false";
                builder.Configuration["Notifications:Email:FromAddress"] = "notifications@example.test";
                builder.Configuration["Notifications:Email:TrackingBaseUrl"] = "https://example.test";
                builder.Services.AddSingleton<IUserEmailDirectory, TestUserEmailDirectory>();
                builder.Services.AddSingleton<IMerchantContactDirectory, TestMerchantContactDirectory>();
                builder.Services.AddNotificationsBusiness(builder.Configuration);
                builder.Services.AddSingleton<IIntegrationEventHandler<ParcelStatusChangedIntegrationEvent>>(
                    new RecordingHandler(received.Writer));
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

    private sealed class RecordingHandler(ChannelWriter<ParcelStatusChangedIntegrationEvent> received)
        : IIntegrationEventHandler<ParcelStatusChangedIntegrationEvent>
    {
        public Task HandleAsync(ParcelStatusChangedIntegrationEvent evt, CancellationToken ct)
        {
            received.TryWrite(evt);
            return Task.CompletedTask;
        }
    }

    private sealed class TestUserEmailDirectory : IUserEmailDirectory
    {
        public Task<string?> GetActiveEmailAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<string?>("merchant@example.test");
    }

    private sealed class TestMerchantContactDirectory : IMerchantContactDirectory
    {
        public Task<Guid?> GetApprovedContactUserIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<Guid?>(id);
    }

    private sealed class TemporaryDatabase : IDisposable
    {
        private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("swiftdrop-notification-routing-");
        public string Path => System.IO.Path.Combine(directory.FullName, "parcels.db");
        public string NotificationsPath => System.IO.Path.Combine(directory.FullName, "notifications.db");
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
