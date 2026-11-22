using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftDrop.Parcels.Business.Lifecycle;
using SwiftDrop.Parcels.Data.Repositories;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Parcels.Business.Outbox;

/// Bound from the "Parcels:Outbox" configuration section.
public sealed class OutboxOptions
{
    public const string SectionName = "Parcels:Outbox";

    public int PollingIntervalMs { get; set; } = 2000;
    public int BatchSize { get; set; } = 50;
    public int MaxAttempts { get; set; } = 10;
}

/// Sends one batch of pending outbox messages to RabbitMQ through the shared IEventPublisher.
internal sealed class OutboxDispatcher(
    IOutboxRepository outbox,
    IEventPublisher publisher,
    TimeProvider clock,
    IOptions<OutboxOptions> options,
    ILogger<OutboxDispatcher> logger)
{
    public async Task<int> DispatchPendingAsync(CancellationToken ct)
    {
        var settings = options.Value;
        var batch = await outbox.GetPendingAsync(settings.BatchSize, settings.MaxAttempts, ct);
        var sent = 0;

        foreach (var message in batch)
        {
            var eventType = ParcelEventTypes.Find(message.RoutingKey);
            if (eventType is null)
            {
                // Not retryable, so it must not hold back the messages behind it.
                message.MarkFailed($"Unknown routing key {message.RoutingKey}.");
                logger.LogError("Outbox message {EventId} has unknown routing key {RoutingKey}.",
                    message.Id, message.RoutingKey);
                continue;
            }

            try
            {
                var integrationEvent = (IntegrationEvent)JsonSerializer.Deserialize(
                    message.Payload, eventType, JsonSerializerOptions.Web)!;
                await publisher.PublishAsync(integrationEvent, ct);
                message.MarkProcessed(clock.GetUtcNow().UtcDateTime);
                sent++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.MarkFailed($"{ex.GetType().Name}: {ex.Message}");
                logger.LogWarning("Publishing outbox message {EventId} ({RoutingKey}) failed, attempt {Attempt}.",
                    message.Id, message.RoutingKey, message.Attempts);
                // Stop here so later events are not sent ahead of this one; the next run retries it.
                break;
            }
        }

        if (batch.Count > 0) await outbox.SaveChangesAsync(ct);
        return sent;
    }
}

/// Polls the outbox in the background, so the request that changed the parcel never
/// waits for (or fails because of) the broker.
internal sealed class OutboxProcessor(
    IServiceScopeFactory scopes,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(options.Value.PollingIntervalMs));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>().DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Parcels outbox run failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
