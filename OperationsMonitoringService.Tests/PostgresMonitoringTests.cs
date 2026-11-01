using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OperationsMonitoringService.Contracts;
using OperationsMonitoringService.Data;
using OperationsMonitoringService.Services;
using System.Net;
using System.Net.Http.Json;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace OperationsMonitoringService.Tests;

// Optional integration suite: a missing PostgreSQL connection is a SKIP, never a pass.
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MONITORING_TEST_POSTGRES")))
            Skip = "Set MONITORING_TEST_POSTGRES to an isolated PostgreSQL test database.";
    }
}

public sealed class PostgresMonitoringTests
{
    private sealed class InsertSignalInterceptor(TaskCompletionSource signal) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO") && command.CommandText.Contains("telemetry_readings"))
                signal.TrySetResult();
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
    private static string Connection => Environment.GetEnvironmentVariable("MONITORING_TEST_POSTGRES")!;

    private static MonitoringDbContext Context() => new(new DbContextOptionsBuilder<MonitoringDbContext>()
        .UseNpgsql(Connection).Options);

    private static TelemetryIngestionService Service(MonitoringDbContext db) =>
        new(db, Options.Create(new MonitoringOptions()), TimeProvider.System);

    private static TelemetryIngestRequest Request(Guid cattleId, Guid deviceId, DateTimeOffset time,
        decimal temperature = 40.2m) => new()
    {
        CattleId = cattleId, DeviceId = deviceId, RecordedAt = time,
        BodyTemperature = temperature, ActivityIndex = 12.3456m,
        Latitude = -13.656, Longitude = -73.388, BatteryLevel = 99.9876m,
        Origin = "EdgeGateway"
    };

    [PostgresFact]
    public async Task NewContextReadsPersistedValuesAndIdenticalHighPrecisionRetrySucceeds()
    {
        var request = Request(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-1), 40.2345m);
        TelemetryIngestResponse first;
        await using (var db = Context())
        {
            await db.Database.EnsureCreatedAsync();
            first = await Service(db).IngestAsync(request);
        }
        await using var reopened = Context();
        var reading = await reopened.Readings.SingleAsync(r => r.Id == first.ReadingId);
        Assert.Equal(request.BodyTemperature, reading.BodyTemperature);
        Assert.Equal(request.ActivityIndex, reading.ActivityIndex);
        Assert.Equal(request.BatteryLevel, reading.BatteryLevel);
        Assert.Equal(DateTimeKind.Utc, reading.RecordedAtUtc.Kind);
        Assert.Equal(0, reading.RecordedAtUtc.Ticks % 10);
        var retry = await Service(reopened).IngestAsync(request);
        Assert.True(retry.Duplicate);
        Assert.Equal(first.ReadingId, retry.ReadingId);
        Assert.Equal(1, await reopened.Alerts.CountAsync(a => a.CattleId == request.CattleId));
    }

    [PostgresFact]
    public async Task ConcurrentReadingsForOneAnimalCreateOneAlertWithoutLostCounts()
    {
        var cattleId = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow.AddMinutes(-5);
        await using (var setup = Context()) await setup.Database.EnsureCreatedAsync();
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async index =>
        {
            await using var db = Context();
            return await Service(db).IngestAsync(Request(cattleId, Guid.NewGuid(), time, 40.2m + index * 0.001m));
        }));
        await using var check = Context();
        var alert = await check.Alerts.SingleAsync(a => a.CattleId == cattleId);
        Assert.Equal(8, alert.ReadingCount);
        Assert.Equal(40.207m, alert.MaximumTemperatureCelsius);
        Assert.Equal(8, await check.Readings.CountAsync(r => r.CattleId == cattleId));
        Assert.Single(results, result => result.AlertRaised);
    }

    [PostgresFact]
    public async Task ConcurrentIdenticalRetriesStoreExactlyOneReading()
    {
        var request = Request(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-5));
        await using (var setup = Context()) await setup.Database.EnsureCreatedAsync();
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var db = Context();
            return await Service(db).IngestAsync(request);
        }));
        Assert.Single(results, result => !result.Duplicate);
        Assert.Single(results.Select(result => result.ReadingId).Distinct());
        await using var check = Context();
        Assert.Equal(1, await check.Readings.CountAsync(r => r.CattleId == request.CattleId));
        Assert.Equal(1, (await check.Alerts.SingleAsync(a => a.CattleId == request.CattleId)).ReadingCount);
    }

    [PostgresFact]
    public async Task ConcurrentConflictingRetryAcrossAnimalsDoesNotLeaveAnOrphanAlert()
    {
        var device = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow.AddMinutes(-5);
        var animals = new[] { Guid.NewGuid(), Guid.NewGuid() };
        await using (var setup = Context()) await setup.Database.EnsureCreatedAsync();
        var outcomes = await Task.WhenAll(animals.Select(async animal =>
        {
            await using var db = Context();
            try
            {
                await Service(db).IngestAsync(Request(animal, device, time));
                return "created";
            }
            catch (TelemetryConflictException) { return "conflict"; }
        }));
        Assert.Single(outcomes, o => o == "created");
        Assert.Single(outcomes, o => o == "conflict");
        await using var check = Context();
        Assert.Equal(1, await check.Readings.CountAsync(r => r.DeviceId == device));
        Assert.Equal(1, await check.Alerts.CountAsync(a => animals.Contains(a.CattleId)));
    }

    [PostgresFact]
    public async Task UniqueConflictFromAnExternalWriterRollsBackBeforeRetryLookup()
    {
        var request = Request(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-5), 39.0m);
        await using var external = Context();
        await external.Database.EnsureCreatedAsync();
        await using var transaction = await external.Database.BeginTransactionAsync();
        var utc = request.RecordedAt.UtcDateTime;
        var persisted = new TelemetryReading
        {
            Id = Guid.NewGuid(), DeviceId = request.DeviceId, CattleId = request.CattleId,
            BodyTemperature = request.BodyTemperature, ActivityIndex = request.ActivityIndex!.Value,
            BatteryLevel = request.BatteryLevel, Latitude = request.Latitude!.Value,
            Longitude = request.Longitude!.Value, Origin = request.Origin,
            RecordedAtUtc = utc.AddTicks(-(utc.Ticks % 10)), ReceivedAtUtc = DateTime.UtcNow
        };
        external.Readings.Add(persisted);
        await external.SaveChangesAsync();
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var concurrent = new MonitoringDbContext(new DbContextOptionsBuilder<MonitoringDbContext>()
            .UseNpgsql(Connection).AddInterceptors(new InsertSignalInterceptor(signal)).Options);
        var ingestion = Service(concurrent).IngestAsync(request);
        await signal.Task.WaitAsync(TimeSpan.FromSeconds(15));
        await transaction.CommitAsync();
        var retry = await ingestion;
        Assert.True(retry.Duplicate);
        Assert.Equal(persisted.Id, retry.ReadingId);
        Assert.Null(concurrent.Database.CurrentTransaction);
    }

    [PostgresFact]
    public async Task NonDevelopmentHostRequiresApiKeyForBothEndpoints()
    {
        // Test-only credential, not a deployment credential.
        const string testKey = "integration-test-only";
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Staging");
            builder.UseSetting("ConnectionStrings:Monitoring", Connection);
            builder.UseSetting("Security:ApiKey", testKey);
        });
        using var client = factory.CreateClient();
        var request = Request(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-5));
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/v1/telemetry", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/alerts")).StatusCode);
        client.DefaultRequestHeaders.Add("X-API-Key", "incorrect-key");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/alerts")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-API-Key");
        client.DefaultRequestHeaders.Add("X-API-Key", testKey);
        Assert.Equal(HttpStatusCode.Created,
            (await client.PostAsJsonAsync("/api/v1/telemetry", request)).StatusCode);
        var alerts = await client.GetFromJsonAsync<List<AlertResponse>>($"/api/v1/alerts?cattleId={request.CattleId}");
        Assert.Single(alerts!);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }
}
