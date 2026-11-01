using Microsoft.EntityFrameworkCore;
using OperationsMonitoringService.Contracts;
using OperationsMonitoringService.Data;

namespace OperationsMonitoringService.Services;

public sealed class AlertQueryService(MonitoringDbContext database)
{
    public async Task<IReadOnlyList<AlertResponse>> ListAsync(
        Guid? cattleId, string status, int limit, int offset,
        CancellationToken cancellationToken = default)
    {
        if (status is not ("ACTIVE" or "ALL"))
            throw new ArgumentException("status must be ACTIVE or ALL.");
        if (limit is < 1 or > 100)
            throw new ArgumentException("limit must be between 1 and 100.");
        if (offset < 0)
            throw new ArgumentException("offset must be greater than or equal to zero.");

        var query = database.Alerts.AsNoTracking();
        if (status == "ACTIVE")
            query = query.Where(alert => alert.Status == "ACTIVE");
        if (cattleId is not null)
            query = query.Where(alert => alert.CattleId == cattleId.Value);

        return await query.OrderByDescending(alert => alert.CreatedAtUtc)
            .ThenByDescending(alert => alert.Id)
            .Skip(offset).Take(limit)
            .Select(alert => new AlertResponse(alert.Id, alert.CattleId, alert.Type,
                alert.Status, alert.MaximumTemperatureCelsius, alert.ThresholdCelsius,
                alert.ReadingCount, alert.StartedAtUtc, alert.LastObservedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
