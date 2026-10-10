using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OperationsMonitoringService.Contracts;
using OperationsMonitoringService.Data;
using OperationsMonitoringService.Services;

namespace OperationsMonitoringService.Tests;

public sealed class TelemetryIngestionTests
{
    private static readonly Guid DeviceId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
    private static readonly Guid CattleId = Guid.Parse("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d");

    [Fact]
    public async Task HotReadingCreatesOneFeverAlert()
    {
        await using var database = CreateDatabase();
        var result = await CreateService(database).IngestAsync(Request(40.2m));

        Assert.True(result.AlertRaised);
        Assert.Equal("Fever", result.AlertType);
        Assert.Equal(1, await database.Alerts.CountAsync());
        Assert.Equal(1, await database.Readings.CountAsync());
        Assert.Equal(40.2m, (await database.Alerts.SingleAsync()).MaximumTemperatureCelsius);
    }

    [Fact]
    public async Task RetriedReadingReturnsOriginalResultWithoutDuplicate()
    {
        await using var database = CreateDatabase();
        var service = CreateService(database);
        var request = Request(40.2m);

        var first = await service.IngestAsync(request);
        var retry = await service.IngestAsync(request);

        Assert.True(retry.Duplicate);
        Assert.False(retry.AlertRaised);
        Assert.Equal(first.ReadingId, retry.ReadingId);
        Assert.Equal(first.AlertId, retry.AlertId);
        Assert.Equal(1, await database.Readings.CountAsync());
        Assert.Equal(1, await database.Alerts.CountAsync());
    }

    [Fact]
    public async Task ReusingTheReadingKeyWithDifferentValuesIsRejected()
    {
        await using var database = CreateDatabase();
        var service = CreateService(database);
        var recordedAt = DateTimeOffset.UtcNow;
        await service.IngestAsync(Request(39.0m, recordedAt));

        await Assert.ThrowsAsync<TelemetryConflictException>(
            () => service.IngestAsync(Request(40.2m, recordedAt)));

        Assert.Equal(1, await database.Readings.CountAsync());
        Assert.Empty(database.Alerts);
    }

    [Fact]
    public async Task HotReadingsWithinSixHoursUpdateSameAlert()
    {
        await using var database = CreateDatabase();
        var service = CreateService(database);
        var start = DateTimeOffset.UtcNow.AddHours(-2);

        var first = await service.IngestAsync(Request(40.0m, start));
        var second = await service.IngestAsync(Request(40.5m, start.AddHours(1)));

        Assert.True(first.AlertRaised);
        Assert.False(second.AlertRaised);
        Assert.Equal(first.AlertId, second.AlertId);
        Assert.Equal(2, (await database.Alerts.SingleAsync()).ReadingCount);
        Assert.Equal(40.5m, (await database.Alerts.SingleAsync()).MaximumTemperatureCelsius);
    }

    [Fact]
    public async Task HotReadingOutsideSixHourWindowCreatesAnotherAlert()
    {
        await using var database = CreateDatabase();
        var service = CreateService(database);
        var start = DateTimeOffset.UtcNow.AddHours(-8);

        await service.IngestAsync(Request(40.0m, start));
        var later = await service.IngestAsync(Request(40.1m, start.AddHours(7)));

        Assert.True(later.AlertRaised);
        Assert.Equal(2, await database.Alerts.CountAsync());
    }

    [Fact]
    public async Task NormalReadingCreatesNoAlert()
    {
        await using var database = CreateDatabase();
        var result = await CreateService(database).IngestAsync(Request(39.0m));

        Assert.False(result.AlertRaised);
        Assert.Null(result.AlertId);
        Assert.Empty(database.Alerts);
    }

    [Fact]
    public void MissingDeviceAndTimestampFailValidation()
    {
        var request = new TelemetryIngestRequest
        {
            CattleId = CattleId,
            BodyTemperature = 40.2m,
            ActivityIndex = 12,
            Latitude = -13.656,
            Longitude = -73.388
        };
        var errors = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(request, new ValidationContext(request), errors, true);

        Assert.False(valid);
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.DeviceId)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.RecordedAt)));
    }

    private static MonitoringDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<MonitoringDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MonitoringDbContext(options);
    }

    private static TelemetryIngestionService CreateService(MonitoringDbContext database) =>
        new(database, Options.Create(new MonitoringOptions { FeverThresholdCelsius = 39.5m }),
            TimeProvider.System);

    private static TelemetryIngestRequest Request(decimal temperature, DateTimeOffset? recordedAt = null) =>
        new()
        {
            DeviceId = DeviceId,
            CattleId = CattleId,
            BodyTemperature = temperature,
            ActivityIndex = 12,
            Latitude = -13.656,
            Longitude = -73.388,
            RecordedAt = recordedAt ?? DateTimeOffset.UtcNow
        };
}
