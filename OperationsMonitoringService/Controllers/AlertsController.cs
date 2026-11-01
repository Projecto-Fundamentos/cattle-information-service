using Microsoft.AspNetCore.Mvc;
using OperationsMonitoringService.Contracts;
using OperationsMonitoringService.Services;

namespace OperationsMonitoringService.Controllers;

[ApiController]
[Route("api/v1/alerts")]
public sealed class AlertsController(AlertQueryService queries) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AlertResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AlertResponse>>> Get(
        [FromQuery] Guid? cattleId,
        [FromQuery] string status = "ACTIVE",
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await queries.ListAsync(cattleId, status, limit, offset, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ApiError(exception.Message));
        }
    }
}
