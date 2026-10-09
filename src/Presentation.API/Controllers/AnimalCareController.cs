using Core.Application.Livestock;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;

namespace Presentation.API.Controllers;

[ApiController]
[Route("api/animals/{animalId:guid}")]
public sealed class AnimalCareController(AnimalCareService care, AnimalReproductionService reproduction, AnimalProductionService production) : ControllerBase
{
    [HttpGet("production"), HasPermission("animals.get"), HasPermission("production.list")]
    public async Task<IActionResult> Production(Guid animalId, [FromQuery] CarePageRequest query, CancellationToken ct) => Ok(await production.GetAsync(animalId, query, ct));
    [HttpPost("production"), HasPermission("animals.get"), HasPermission("production.create")]
    public async Task<IActionResult> RecordProduction(Guid animalId, AnimalProductionRequest request, CancellationToken ct)
    {
        var result = await production.RecordAsync(animalId, request, ct);
        return StatusCode(result.Replayed ? 200 : 201, result);
    }
    [HttpGet("clinical"), HasPermission("animals.get"), HasPermission("clinical.list")]
    public async Task<IActionResult> Clinical(Guid animalId, [FromQuery] CarePageRequest query, CancellationToken ct) => Ok(await care.GetAsync(animalId, query, ct));
    [HttpPost("clinical"), HasPermission("animals.get"), HasPermission("clinical.create")]
    public async Task<IActionResult> RecordClinical(Guid animalId, ClinicalRequest request, CancellationToken ct)
    {
        var result = await care.RecordAsync(animalId, request, ct);
        return StatusCode(result.Replayed ? 200 : 201, result);
    }
    [HttpGet("reproduction"), HasPermission("animals.get"), HasPermission("reproduction.list")]
    public async Task<IActionResult> Reproduction(Guid animalId, [FromQuery] CarePageRequest query, CancellationToken ct) => Ok(await reproduction.GetAsync(animalId, query, ct));
    [HttpPost("reproduction"), HasPermission("animals.get"), HasPermission("reproduction.create")]
    public async Task<IActionResult> RecordReproduction(Guid animalId, ReproductiveRequest request, CancellationToken ct)
    {
        var result = await reproduction.RecordAsync(animalId, request, ct);
        return StatusCode(result.Replayed ? 200 : 201, result);
    }
}
