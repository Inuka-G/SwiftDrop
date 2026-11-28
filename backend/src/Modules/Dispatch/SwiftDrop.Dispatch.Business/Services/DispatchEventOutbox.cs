using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SwiftDrop.Dispatch.Data.Repositories;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Dispatch.Business.Services;

internal sealed class DispatchOutboxProcessor(IServiceScopeFactory scopes, ILogger<DispatchOutboxProcessor> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IDispatchRepository>();
                var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
                foreach (var item in await repository.GetPendingOutboxAsync(50, stoppingToken))
                {
                    try
                    {
                        await publisher.PublishAsync(item.EventId, item.RoutingKey, item.Payload, stoppingToken);
                        await repository.MarkOutboxProcessedAsync(item.EventId, stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        await repository.RecordOutboxFailureAsync(item.EventId, stoppingToken);
                        logger.LogWarning("Dispatch event {EventId} publish failed ({ErrorType}).", item.EventId, ex.GetType().Name);
                        break;
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Dispatch outbox processing failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
