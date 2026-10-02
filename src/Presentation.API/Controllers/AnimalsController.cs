using Core.Application.Livestock;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;

namespace Presentation.API.Controllers;

[ApiController]
[Route("api/animals")]
public sealed class AnimalsController(IAnimalQueryService animals) : ControllerBase
{
    [HttpGet]
    [HasPermission("animals.list")]
    public async Task<ActionResult<IReadOnlyList<AnimalListItem>>> List(CancellationToken cancellationToken) =>
        Ok(await animals.ListAsync(cancellationToken));

    [HttpGet("stale")]
    [HasPermission("animals.list")]
    public async Task<ActionResult<IReadOnlyList<AnimalListItem>>> ListStale(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (days <= 0)
        {
            throw new ArgumentException("The 'days' query parameter must be greater than zero.", nameof(days));
        }

        return Ok(await animals.ListStaleAsync(days, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("animals.get")]
    public async Task<ActionResult<AnimalDetail>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await animals.GetAsync(id, cancellationToken));
}
