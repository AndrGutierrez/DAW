using Core.Application.Livestock;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;

namespace Presentation.API.Controllers;

[ApiController]
[Route("api/animals/{animalId:guid}/movements")]
public sealed class AnimalMovementController(AnimalMovementService service) : ControllerBase
{
    [HttpGet, HasPermission("animals.get"), HasPermission("paddocks.list")]
    public async Task<IActionResult> Get(Guid animalId, [FromQuery] CarePageRequest query, CancellationToken ct) => Ok(await service.GetAsync(animalId, query, ct));
    [HttpPost, HasPermission("animals.get"), HasPermission("animals.update")]
    public async Task<IActionResult> Create(Guid animalId, AnimalMovementRequest request, CancellationToken ct)
    {
        var result = await service.RecordAsync(animalId, request, ct);
        return StatusCode(result.Replayed ? 200 : 201, result);
    }
}
