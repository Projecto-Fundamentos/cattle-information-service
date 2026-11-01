using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using OperationsMonitoringService.Contracts;
using OperationsMonitoringService.Data;

namespace OperationsMonitoringService.Services;

public sealed class TelemetryIngestionService(
    MonitoringDbContext database,
    IOptions<MonitoringOptions> options,
    TimeProvider clock)
{
    public async Task<TelemetryIngestResponse> IngestAsync(
        TelemetryIngestRequest request,
        CancellationToken cancellationToken = default)
    {
        // PostgreSQL timestamps have microsecond resolution. Normalize before both
        // lookup and persistence, including the in-memory demonstration provider.
        var utc = request.RecordedAt.UtcDateTime;
        var recordedAtUtc = utc.AddTicks(-(utc.Ticks % 10));
        await using var writeScope = await MonitoringWriteScope.BeginAsync(
            database, request.CattleId, request.DeviceId, recordedAtUtc, cancellationToken);

        var existing = await database.Readings.AsNoTracking()
            .FirstOrDefaultAsync(reading => reading.DeviceId == request.DeviceId &&
                reading.RecordedAtUtc == recordedAtUtc, cancellationToken);
        if (existing is not null)
            return await DuplicateResponseAsync(existing, request, cancellationToken);

        var receivedAtUtc = clock.GetUtcNow().UtcDateTime;
        var reading = new TelemetryReading
        {
            Id = Guid.NewGuid(),
            DeviceId = request.DeviceId,
            CattleId = request.CattleId,
            BodyTemperature = request.BodyTemperature,
            ActivityIndex = request.ActivityIndex!.Value,
            RuminationMinutes = request.RuminationMinutes,
            Latitude = request.Latitude!.Value,
            Longitude = request.Longitude!.Value,
            BatteryLevel = request.BatteryLevel,
            Origin = request.Origin,
            RecordedAtUtc = recordedAtUtc,
            ReceivedAtUtc = receivedAtUtc
        };
        database.Readings.Add(reading);

        MonitoringAlert? alert = null;
        var alertRaised = false;
        if (reading.BodyTemperature > options.Value.FeverThresholdCelsius)
        {
            var windowStart = recordedAtUtc.AddHours(-6);
            alert = await database.Alerts
                .Where(candidate => candidate.CattleId == reading.CattleId &&
                    candidate.Type == "FEVER" && candidate.Status == "ACTIVE" &&
                    candidate.StartedAtUtc >= windowStart && candidate.StartedAtUtc <= recordedAtUtc)
                .OrderByDescending(candidate => candidate.StartedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (alert is null)
            {
                alert = new MonitoringAlert
                {
                    Id = Guid.NewGuid(),
                    CattleId = reading.CattleId,
                    TriggerReadingId = reading.Id,
                    ThresholdCelsius = options.Value.FeverThresholdCelsius,
                    MaximumTemperatureCelsius = reading.BodyTemperature,
                    ReadingCount = 1,
                    StartedAtUtc = recordedAtUtc,
                    LastObservedAtUtc = recordedAtUtc,
                    CreatedAtUtc = receivedAtUtc
                };
                database.Alerts.Add(alert);
                alertRaised = true;
            }
            else
            {
                alert.ReadingCount++;
                if (reading.BodyTemperature > alert.MaximumTemperatureCelsius)
                    alert.MaximumTemperatureCelsius = reading.BodyTemperature;
                if (recordedAtUtc > alert.LastObservedAtUtc)
                    alert.LastObservedAtUtc = recordedAtUtc;
            }

            reading.AlertId = alert.Id;
        }

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            await writeScope.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // A non-cooperating writer may still race with this process. Roll back
            // the failed transaction before looking up a committed retry key.
            await writeScope.RollbackAsync(cancellationToken);
            database.ChangeTracker.Clear();
            existing = await database.Readings.AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.DeviceId == request.DeviceId &&
                    candidate.RecordedAtUtc == recordedAtUtc, cancellationToken);
            if (existing is null)
                throw;
            return await DuplicateResponseAsync(existing, request, cancellationToken);
        }

        return new TelemetryIngestResponse(reading.Id, alertRaised, alert?.Id,
            alert is null ? null : "Fever", false);
    }

    private async Task<TelemetryIngestResponse> DuplicateResponseAsync(
        TelemetryReading reading,
        TelemetryIngestRequest request,
        CancellationToken cancellationToken)
    {
        if (reading.CattleId != request.CattleId ||
            reading.BodyTemperature != request.BodyTemperature ||
            reading.ActivityIndex != request.ActivityIndex ||
            reading.RuminationMinutes != request.RuminationMinutes ||
            reading.Latitude != request.Latitude ||
            reading.Longitude != request.Longitude ||
            reading.BatteryLevel != request.BatteryLevel ||
            reading.Origin != request.Origin)
            throw new TelemetryConflictException();

        var alert = reading.AlertId is null ? null : await database.Alerts.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == reading.AlertId.Value, cancellationToken);
        return new TelemetryIngestResponse(reading.Id, false, alert?.Id,
            alert is null ? null : "Fever", true);
    }
}
