using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SwiftDrop.SharedKernel.Messaging;

internal sealed class EventConsumerService<TEvent>(
    string queueName,
    string routingKey,
    string? previousRoutingKey,
    Type? handlerType,
    RabbitMqConnection connection,
    IServiceScopeFactory scopes,
    ILogger<EventConsumerService<TEvent>> logger) : BackgroundService
    where TEvent : class
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IChannel? channel = null;
            try
            {
                channel = await SetupAsync(stoppingToken);

                var channelClosedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                channel.ChannelShutdownAsync += (_, _) =>
                {
                    channelClosedTcs.TrySetResult();
                    return Task.CompletedTask;
                };

                using var reg = stoppingToken.Register(() => channelClosedTcs.TrySetCanceled());
                await channelClosedTcs.Task;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("RabbitMQ not ready or connection lost for {Queue}: {Message}. Retrying in 5s.", queueName, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            finally
            {
                if (channel is not null)
                {
                    try { await channel.DisposeAsync(); }
                    catch { /* ignore cleanup error */ }
                }
            }
        }
    }

    private async Task<IChannel> SetupAsync(CancellationToken ct)
    {
        var opts = connection.Options;
        var conn = await connection.GetAsync(ct);
        var channel = await conn.CreateChannelAsync(cancellationToken: ct);

        await connection.DeclareExchangesAsync(channel, ct);

        // dead letter queue: messages that keep failing end up here instead of being lost
        // Keep the original DLX argument so RabbitMQ accepts existing durable queues.
        var deadLetterRoutingKey = previousRoutingKey ?? routingKey;
        var dlq = $"{queueName}.dlq";
        await channel.QueueDeclareAsync(dlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(dlq, opts.DeadLetterExchange, deadLetterRoutingKey, cancellationToken: ct);

        // main queue: one durable queue per consuming module
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = opts.DeadLetterExchange,
                ["x-dead-letter-routing-key"] = deadLetterRoutingKey
            }, cancellationToken: ct);
        await channel.QueueBindAsync(queueName, opts.Exchange, routingKey, cancellationToken: ct);

        if (previousRoutingKey is not null)
            await channel.QueueUnbindAsync(queueName, opts.Exchange, previousRoutingKey, cancellationToken: ct);

        await channel.BasicQosAsync(0, prefetchCount: 10, global: false, cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, ea) => OnReceivedAsync(channel, ea, ct);
        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer, ct);

        logger.LogInformation("Consuming {Event} on queue {Queue}", typeof(TEvent).Name, queueName);
        return channel;
    }

    private async Task OnReceivedAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        // Preserve any legacy messages already queued without interpreting them as the new event.
        if (ea.RoutingKey != routingKey)
        {
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, ct);
            return;
        }

        var attempts = connection.Options.MaxHandlerAttempts;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                var @event = JsonSerializer.Deserialize<TEvent>(ea.Body.Span, JsonSerializerOptions.Web)
                    ?? throw new InvalidOperationException("Empty event payload.");

                using var scope = scopes.CreateScope();
                await DispatchHandlersAsync(scope.ServiceProvider, @event, handlerType, ct);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // log type and id only: never log the payload (personal data)
                logger.LogWarning("Handler failed for {Event} (attempt {Attempt}/{Max}): {Error}",
                    typeof(TEvent).Name, attempt, attempts, ex.GetType().Name);
                if (attempt < attempts)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct); // 2s, 4s backoff
            }
        }

        // all attempts failed: reject without requeue, so the broker routes it to the DLQ
        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, ct);
        logger.LogError("{Event} sent to dead letter queue {Queue}.dlq", typeof(TEvent).Name, queueName);
    }

    internal static async Task DispatchHandlersAsync(
        IServiceProvider services,
        TEvent @event,
        Type? handlerType,
        CancellationToken ct)
    {
        var registeredHandlers = services.GetServices<IIntegrationEventHandler<TEvent>>();
        var handlers = handlerType is null
            ? registeredHandlers.ToArray()
            : registeredHandlers.Where(handlerType.IsInstanceOfType).ToArray();

        if (handlerType is not null && handlers.Length != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one {handlerType.Name} handler for {typeof(TEvent).Name}, found {handlers.Length}.");
        }

        foreach (var handler in handlers)
            await handler.HandleAsync(@event, ct);
    }
}
