using Microsoft.AspNetCore.Mvc;
using OperationsMonitoringService.Contracts;
using OperationsMonitoringService.Services;

namespace OperationsMonitoringService.Controllers;

[ApiController]
[Route("api/v1/telemetry")]
public sealed class TelemetryController(TelemetryIngestionService ingestion) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TelemetryIngestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(TelemetryIngestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TelemetryIngestResponse>> Ingest(
        [FromBody] TelemetryIngestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ingestion.IngestAsync(request, cancellationToken);
            return result.Duplicate ? Ok(result) : StatusCode(StatusCodes.Status201Created, result);
        }
        catch (TelemetryConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }
}
