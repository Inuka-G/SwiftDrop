using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Reporting.Api.Dtos;
using SwiftDrop.Reporting.Business.Consumers;
using SwiftDrop.Reporting.Business.Services;
using SwiftDrop.Reporting.Contracts;
using SwiftDrop.Reporting.Data;
using SwiftDrop.Reporting.Data.Persistence;
using SwiftDrop.Reporting.Data.Repositories;

namespace SwiftDrop.Reporting.UnitTests;

public sealed class ReportingTests
{
    [Fact]
    public void Utc_resolver_uses_utc_calendar_day()
    {
        var resolver = new ReportDateResolver();
        Assert.Equal(new DateOnly(2025, 12, 31), resolver.FromUtc(new DateTimeOffset(2026, 1, 1, 0, 30, 0, TimeSpan.FromHours(2))));
        Assert.Equal(new DateOnly(2026, 1, 2), resolver.FromUtc(new DateTimeOffset(2026, 1, 1, 23, 30, 0, TimeSpan.FromHours(-2))));
    }

    [Fact]
    public async Task Reports_zero_fill_dates_and_calculate_rates_from_terminal_outcomes()
    {
        await using var harness = new Harness();
        var repository = harness.Provider.GetRequiredService<IReportingRepository>();
        var date = new DateOnly(2026, 2, 3);
        await repository.ApplyEventAsync(Guid.NewGuid(), date, 4, 0, 0, default);
        await repository.ApplyEventAsync(Guid.NewGuid(), date, 0, 3, 0, default);
        await repository.ApplyEventAsync(Guid.NewGuid(), date, 0, 0, 1, default);
        var service = harness.Provider.GetRequiredService<IReportService>();

        var daily = await service.GetDeliveriesPerDayAsync(date, date.AddDays(1), default);
        Assert.True(daily.IsSuccess);
        Assert.Collection(daily.Value!.Days,
            row => { Assert.Equal(date, row.Date); Assert.Equal(4, row.Created); Assert.Equal(3, row.Delivered); Assert.Equal(1, row.Failed); },
            row => { Assert.Equal(date.AddDays(1), row.Date); Assert.Equal(0, row.Created); });

        var rate = await service.GetSuccessFailureRateAsync(date, date, default);
        Assert.Equal(4, rate.Value!.TotalCount);
        Assert.Equal(0.75m, rate.Value.SuccessRate);
    }

    [Fact]
    public async Task Empty_day_has_undefined_success_rate()
    {
        await using var harness = new Harness();
        var report = await harness.Provider.GetRequiredService<IReportService>()
            .GetSuccessFailureRateAsync(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 1), default);
        Assert.True(report.IsSuccess);
        Assert.Null(report.Value!.SuccessRate);
        Assert.Equal(0, report.Value.TotalCount);
    }

    [Fact]
    public async Task Dashboard_summary_has_frontend_contract_names_and_date_format()
    {
        await using var harness = new Harness();
        var date = new DateOnly(2026, 3, 1);
        await harness.Provider.GetRequiredService<IReportingRepository>().ApplyEventAsync(Guid.NewGuid(), date, 2, 0, 0, default);
        var summary = await harness.Provider.GetRequiredService<IReportService>().GetSummaryAsync(default);
        var json = System.Text.Json.JsonSerializer.Serialize(summary, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        Assert.Contains("\"date\":", json);
        Assert.Contains("\"created\":", json);
        Assert.Contains("\"delivered\":", json);
        Assert.Contains("\"failed\":", json);
        Assert.Contains("\"successRate\":", json);
        Assert.DoesNotContain("createdCount", json);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("2026-03-02", "2026-03-01")]
    [InlineData("2025-01-01", "2026-01-02")]
    public void Validator_rejects_invalid_ranges(string? fromText, string? toText)
    {
        var validator = new ReportRangeRequestValidator();
        var request = new ReportRangeRequest(Parse(fromText), Parse(toText));
        Assert.False(validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validator_accepts_one_day_range()
    {
        var date = new DateOnly(2026, 3, 1);
        Assert.True(new ReportRangeRequestValidator().Validate(new ReportRangeRequest(date, date)).IsValid);
    }

    [Fact]
    public async Task Event_consumer_is_idempotent_and_counts_only_delivered_and_failed_statuses()
    {
        await using var harness = new Harness();
        var repository = harness.Provider.GetRequiredService<IReportingRepository>();
        var resolver = new ReportDateResolver();
        var consumer = new ParcelStatusChangedConsumer(repository, resolver);
        var date = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero);
        var eventId = Guid.NewGuid();
        await consumer.HandleAsync(new(eventId, date, "IN_TRANSIT"), default);
        await consumer.HandleAsync(new(eventId, date, "DELIVERED"), default);
        await consumer.HandleAsync(new(Guid.NewGuid(), date, "DELIVERED"), default);
        var report = await harness.Provider.GetRequiredService<IReportService>()
            .GetSuccessFailureRateAsync(new DateOnly(2026, 4, 2), new DateOnly(2026, 4, 2), default);
        Assert.Equal(1, report.Value!.DeliveredCount);
        Assert.Equal(0, report.Value.FailedCount);
    }

    [Fact]
    public async Task Concurrent_events_for_the_same_day_do_not_lose_increments()
    {
        var path = Path.Combine(Path.GetTempPath(), $"reporting-concurrent-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<ReportingDbContext>().UseSqlite($"Data Source={path};Default Timeout=30").Options;
        try
        {
            await using (var setup = new ReportingDbContext(options)) await setup.Database.EnsureCreatedAsync();
            var date = new DateOnly(2026, 5, 4);
            var first = Task.Run(async () =>
            {
                await using var db = new ReportingDbContext(options);
                await new ReportingRepository(db).ApplyEventAsync(Guid.NewGuid(), date, 1, 0, 0, default);
            });
            var second = Task.Run(async () =>
            {
                await using var db = new ReportingDbContext(options);
                await new ReportingRepository(db).ApplyEventAsync(Guid.NewGuid(), date, 1, 0, 0, default);
            });
            await Task.WhenAll(first, second);
            await using var verify = new ReportingDbContext(options);
            Assert.Equal(2, (await verify.DailyStatistics.SingleAsync(x => x.Date == date)).CreatedCount);
        }
        finally
        {
            try { File.Delete(path); } catch (IOException) { }
        }
    }

    private static DateOnly? Parse(string? value) => value is null ? null : DateOnly.Parse(value);

    private sealed class Harness : IAsyncDisposable
    {
        private readonly string path = Path.Combine(Path.GetTempPath(), $"reporting-{Guid.NewGuid():N}.db");
        public ServiceProvider Provider { get; }
        public Harness()
        {
            var services = new ServiceCollection();
            services.AddDbContext<ReportingDbContext>(options => options.UseSqlite($"Data Source={path}"));
            services.AddScoped<IReportingRepository, ReportingRepository>();
            services.AddSingleton<IReportDateResolver, ReportDateResolver>();
            services.AddSingleton<TimeProvider>(TimeProvider.System);
            services.AddScoped<IReportService, ReportService>();
            Provider = services.BuildServiceProvider();
            Provider.GetRequiredService<ReportingDbContext>().Database.EnsureCreated();
        }
        public async ValueTask DisposeAsync()
        {
            await Provider.DisposeAsync();
            try { File.Delete(path); } catch (IOException) { }
        }
    }
}
