using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperationsMonitoringService.Contracts;
using OperationsMonitoringService.Data;

namespace OperationsMonitoringService.Controllers;

[ApiController]
[Route("api/v1/alerts")]
public sealed class AlertsController(MonitoringDbContext database) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AlertResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AlertResponse>>> Get(
        [FromQuery] Guid? cattleId,
        [FromQuery] string status = "ACTIVE",
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (status is not ("ACTIVE" or "ALL"))
            return BadRequest(new { error = "status must be ACTIVE or ALL." });
        if (limit is < 1 or > 100)
            return BadRequest(new { error = "limit must be between 1 and 100." });

        var query = database.Alerts.AsNoTracking();
        if (status == "ACTIVE")
            query = query.Where(alert => alert.Status == "ACTIVE");
        if (cattleId is not null)
            query = query.Where(alert => alert.CattleId == cattleId.Value);

        var alerts = await query.OrderByDescending(alert => alert.CreatedAtUtc)
            .Take(limit)
            .Select(alert => new AlertResponse(alert.Id, alert.CattleId, alert.Type,
                alert.Status, alert.MaximumTemperatureCelsius, alert.ThresholdCelsius,
                alert.ReadingCount, alert.StartedAtUtc, alert.LastObservedAtUtc))
            .ToListAsync(cancellationToken);
        return Ok(alerts);
    }
}
