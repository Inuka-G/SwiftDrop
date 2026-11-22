using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SwiftDrop.Parcels.Business.Outbox;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Repositories;
using SwiftDrop.Parcels.UnitTests.TestSupport;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Parcels.UnitTests;

public sealed class OutboxDispatcherTests : IDisposable
{
    private readonly ParcelsTestDb db = new();
    private readonly RecordingPublisher publisher = new();

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task Publishes_pending_messages_in_order_and_marks_them_processed()
    {
        var first = await AddMessageAsync(minutes: 0);
        var second = await AddMessageAsync(minutes: 1);

        var sent = await Dispatcher().DispatchPendingAsync(default);

        sent.Should().Be(2);
        publisher.Published.Select(e => e.EventId).Should().Equal(first, second);
        await using var check = db.NewContext();
        (await check.OutboxMessages.AllAsync(m => m.ProcessedAtUtc != null)).Should().BeTrue();

        (await Dispatcher().DispatchPendingAsync(default)).Should().Be(0);   // nothing is sent twice
    }

    [Fact]
    public async Task Rebuilds_the_typed_event_with_its_original_id()
    {
        var id = await AddMessageAsync(minutes: 0);

        await Dispatcher().DispatchPendingAsync(default);

        var evt = publisher.Published.Should().ContainSingle().Which.Should().BeOfType<ParcelStatusChanged>().Subject;
        evt.EventId.Should().Be(id);
        evt.NewStatus.Should().Be("PickedUp");
    }

    [Fact]
    public async Task Failure_records_the_attempt_and_holds_back_later_messages()
    {
        var first = await AddMessageAsync(minutes: 0);
        await AddMessageAsync(minutes: 1);
        publisher.FailFor.Add(first);

        var sent = await Dispatcher().DispatchPendingAsync(default);

        sent.Should().Be(0);
        publisher.Published.Should().BeEmpty();
        await using var check = db.NewContext();
        var failed = await check.OutboxMessages.SingleAsync(m => m.Id == first);
        failed.Attempts.Should().Be(1);
        failed.LastError.Should().Contain("broker down");
        (await check.OutboxMessages.CountAsync(m => m.ProcessedAtUtc == null)).Should().Be(2);
    }

    [Fact]
    public async Task Retries_succeed_once_the_broker_recovers()
    {
        var first = await AddMessageAsync(minutes: 0);
        publisher.FailFor.Add(first);
        await Dispatcher().DispatchPendingAsync(default);

        publisher.FailFor.Clear();
        (await Dispatcher().DispatchPendingAsync(default)).Should().Be(1);
        publisher.Published.Select(e => e.EventId).Should().Equal(first);
    }

    [Fact]
    public async Task Gives_up_after_max_attempts()
    {
        var first = await AddMessageAsync(minutes: 0);
        publisher.FailFor.Add(first);
        for (var i = 0; i < 3; i++) await Dispatcher(maxAttempts: 3).DispatchPendingAsync(default);

        publisher.FailFor.Clear();
        (await Dispatcher(maxAttempts: 3).DispatchPendingAsync(default)).Should().Be(0);
    }

    [Fact]
    public async Task Unknown_routing_key_does_not_block_later_messages()
    {
        db.Context.OutboxMessages.Add(new OutboxMessage(Guid.NewGuid(), "parcels.old-name", "{}",
            db.Clock.Now.UtcDateTime));
        await db.Context.SaveChangesAsync();
        var valid = await AddMessageAsync(minutes: 1);

        (await Dispatcher().DispatchPendingAsync(default)).Should().Be(1);
        publisher.Published.Select(e => e.EventId).Should().Equal(valid);
    }

    private OutboxDispatcher Dispatcher(int maxAttempts = 10) =>
        new(new OutboxRepository(db.NewContext()), publisher, db.Clock,
            Options.Create(new OutboxOptions { MaxAttempts = maxAttempts }), NullLogger<OutboxDispatcher>.Instance);

    private async Task<Guid> AddMessageAsync(int minutes)
    {
        var at = db.Clock.Now.UtcDateTime.AddMinutes(minutes);
        var evt = new ParcelStatusChanged(Guid.NewGuid(), "SD-TEST-0001", "Created", "PickedUp", null,
            Guid.NewGuid(), null, Guid.NewGuid()) { OccurredOnUtc = at };
        db.Context.OutboxMessages.Add(new OutboxMessage(evt.EventId, "parcel.status.changed",
            JsonSerializer.Serialize(evt, JsonSerializerOptions.Web), at));
        await db.Context.SaveChangesAsync();
        return evt.EventId;
    }

    private sealed class RecordingPublisher : IEventPublisher
    {
        public List<IntegrationEvent> Published { get; } = [];
        public HashSet<Guid> FailFor { get; } = [];

        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IntegrationEvent
        {
            if (FailFor.Contains(@event.EventId)) throw new InvalidOperationException("broker down");
            Published.Add(@event);
            return Task.CompletedTask;
        }
    }
}
