using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Realtime.UnitTests;

public sealed class EventConsumerHandlerDispatchTests
{
    [Fact]
    public async Task Realtime_queue_delivery_invokes_only_the_realtime_handler_once()
    {
        var realtimeHandler = new RealtimeQueueHandler();
        var webhooksHandler = new WebhooksQueueHandler();
        using var services = CreateServices(realtimeHandler, webhooksHandler);

        await EventConsumerService<ParcelStatusChanged>.DispatchHandlersAsync(
            services,
            CreateEvent(),
            typeof(RealtimeQueueHandler),
            CancellationToken.None);

        realtimeHandler.CallCount.Should().Be(1);
        webhooksHandler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Webhooks_queue_delivery_invokes_only_the_webhooks_handler_once()
    {
        var realtimeHandler = new RealtimeQueueHandler();
        var webhooksHandler = new WebhooksQueueHandler();
        using var services = CreateServices(realtimeHandler, webhooksHandler);

        await EventConsumerService<ParcelStatusChanged>.DispatchHandlersAsync(
            services,
            CreateEvent(),
            typeof(WebhooksQueueHandler),
            CancellationToken.None);

        webhooksHandler.CallCount.Should().Be(1);
        realtimeHandler.CallCount.Should().Be(0);
    }

    private static ServiceProvider CreateServices(
        RealtimeQueueHandler realtimeHandler,
        WebhooksQueueHandler webhooksHandler) =>
        new ServiceCollection()
            .AddSingleton<IIntegrationEventHandler<ParcelStatusChanged>>(realtimeHandler)
            .AddSingleton<IIntegrationEventHandler<ParcelStatusChanged>>(webhooksHandler)
            .BuildServiceProvider();

    private static ParcelStatusChanged CreateEvent() => new(
        Guid.NewGuid(),
        "SD-TEST-10000",
        "Created",
        "InTransit",
        null,
        Guid.NewGuid(),
        null,
        null);

    private sealed class RealtimeQueueHandler : IIntegrationEventHandler<ParcelStatusChanged>
    {
        public int CallCount { get; private set; }

        public Task HandleAsync(ParcelStatusChanged @event, CancellationToken ct)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class WebhooksQueueHandler : IIntegrationEventHandler<ParcelStatusChanged>
    {
        public int CallCount { get; private set; }

        public Task HandleAsync(ParcelStatusChanged @event, CancellationToken ct)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }
}
