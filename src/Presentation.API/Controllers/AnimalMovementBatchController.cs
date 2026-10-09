using Core.Application.Livestock;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;

namespace Presentation.API.Controllers;

[ApiController]
[Route("api/animal-movements/batch")]
public sealed class AnimalMovementBatchController(AnimalMovementService service) : ControllerBase
{
    [HttpPost, HasPermission("animals.get"), HasPermission("animals.update"), HasPermission("paddocks.list"), HasPermission("lots.list")]
    public async Task<IActionResult> Create(AnimalMovementBatchRequest request, CancellationToken ct)
    {
        var result = await service.RecordBatchAsync(request, ct);
        return StatusCode(result.Replayed ? 200 : 201, result);
    }
}
