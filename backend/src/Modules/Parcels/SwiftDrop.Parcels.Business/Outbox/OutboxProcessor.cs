using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

/// Sends one batch of pending outbox messages to the broker.
internal sealed class OutboxDispatcher(
    IOutboxRepository outbox,
    IIntegrationEventPublisher publisher,
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
            try
            {
                await publisher.PublishAsync(message.Id, message.RoutingKey, message.Payload, ct);
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

/// Polls the outbox in the background, so the courier's request never waits for the broker.
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
