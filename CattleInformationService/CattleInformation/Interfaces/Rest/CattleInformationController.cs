using System.Net.Mime;
using CattleInformationService.CattleInformation.Domain.Services.Commands;
using CattleInformationService.CattleInformation.Domain.Services.Queries;
using CattleInformationService.CattleInformation.Interfaces.Rest.Resources;
using CattleInformationService.CattleInformation.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CattleInformationService.CattleInformation.Interfaces.Rest;


[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class CattleInformationController(
    ICattleInformationCommandService  cattleInformationCommandService,
    ICattleInformationQueryService  cattleInformationQueryService
    ):ControllerBase
{
    // Post Create cattle information
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create Cattle Information",
        Description = "Create Cattle Information",
        OperationId = "CreateCattleInformation")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "The product could not be created")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateCattleInformationResource resource)
    {
        var createProductCommand = CreateCattleInformationCommandFromResourceAssembler.ToCommandFromResource(resource);
        var product = await cattleInformationCommandService.Handle(createProductCommand);
        if (product is null)
        {
            return BadRequest();
        }
        return Ok(product);
    }
    // Get CattleInformation information
    
    [HttpGet("{cattleInformationId}", Name = "GetProductById")]
    [SwaggerOperation(
        Summary = "Get cattleInformation By Id",
        Description = "Get a cattleInformation By Id",
        OperationId = "GetCattleInformationById")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "cattleInformation not found")]
    public async Task<IActionResult> GetProductById(int cattleInformationId)
    {
        var result = await cattleInformationQueryService.Handle(cattleInformationId);
        if (result is null)
            return NotFound();
        return Ok(result);
    }
}