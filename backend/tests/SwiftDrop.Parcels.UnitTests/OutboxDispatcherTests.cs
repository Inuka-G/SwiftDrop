using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SwiftDrop.Parcels.Business.Outbox;
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
        publisher.Published.Should().Equal(first, second);
        await using var check = db.NewContext();
        (await check.OutboxMessages.AllAsync(m => m.ProcessedAtUtc != null)).Should().BeTrue();

        (await Dispatcher().DispatchPendingAsync(default)).Should().Be(0);   // nothing is sent twice
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
        publisher.Published.Should().Equal(first);
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

    private OutboxDispatcher Dispatcher(int maxAttempts = 10) =>
        new(new OutboxRepository(db.NewContext()), publisher, db.Clock,
            Options.Create(new OutboxOptions { MaxAttempts = maxAttempts }), NullLogger<OutboxDispatcher>.Instance);

    private async Task<Guid> AddMessageAsync(int minutes)
    {
        var id = Guid.NewGuid();
        db.Context.OutboxMessages.Add(new OutboxMessage(id, "parcels.status-changed", "{}",
            db.Clock.Now.UtcDateTime.AddMinutes(minutes)));
        await db.Context.SaveChangesAsync();
        return id;
    }

    private sealed class RecordingPublisher : IIntegrationEventPublisher
    {
        public List<Guid> Published { get; } = [];
        public HashSet<Guid> FailFor { get; } = [];

        public Task PublishAsync(Guid eventId, string routingKey, string payloadJson, CancellationToken ct)
        {
            if (FailFor.Contains(eventId)) throw new InvalidOperationException("broker down");
            Published.Add(eventId);
            return Task.CompletedTask;
        }
    }
}
