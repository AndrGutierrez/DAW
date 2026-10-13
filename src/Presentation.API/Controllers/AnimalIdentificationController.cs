using Core.Application.Livestock;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;
namespace Presentation.API.Controllers;

[ApiController, Route("api/animals/identify")]
public sealed class AnimalIdentificationController(AnimalIdentificationService service) : ControllerBase
{
    [HttpGet, HasPermission("animals.list"), HasPermission("animals.get")]
    public async Task<IActionResult> Get([FromQuery] Guid farmId, [FromQuery] string identifier, CancellationToken ct) =>
        Ok(await service.IdentifyAsync(farmId, identifier, ct));
}
