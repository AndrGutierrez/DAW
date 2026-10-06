using Core.Application.Livestock;
using Core.Application.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;

namespace Presentation.API.Controllers;

[ApiController]
[Route("api/animals")]
public sealed class AnimalsController(IAnimalQueryService animals, ICrudService<AnimalRequest> writes, AnimalHealthService health, AnimalGrowthService growth, AnimalWeighingService weighing) : ControllerBase
{
    [HttpGet("{id:guid}/growth"), HasPermission("animals.get"), HasPermission("weights.list")]
    public async Task<ActionResult<AnimalGrowthResult>> Growth(Guid id, [FromQuery] GrowthPageRequest request, CancellationToken ct) =>
        Ok(await growth.GetAsync(id, request, ct));

    [HttpPost("{id:guid}/weights"), HasPermission("animals.get"), HasPermission("weights.create")]
    public async Task<ActionResult<AnimalWeighingResult>> Weigh(Guid id, AnimalWeighingRequest request, CancellationToken ct)
    {
        var result = await weighing.RecordAsync(id, request, ct);
        return StatusCode(result.Replayed ? 200 : 201, result);
    }

    [HttpPost, HasPermission("animals.create")]
    public async Task<IActionResult> Create(AnimalRequest request, CancellationToken ct)
    {
        var result = await writes.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}"), HasPermission("animals.update")]
    public async Task<IActionResult> Update(Guid id, AnimalRequest request, CancellationToken ct) => Ok(await writes.UpdateAsync(id, request, ct));

    [HttpPatch("{id:guid}/health"), HasPermission("animals.update")]
    public async Task<IActionResult> UpdateHealth(Guid id, HealthUpdateRequest request, CancellationToken ct)
    {
        await health.UpdateAsync(id, request, ct);
        return Ok(await animals.GetAsync(id, ct));
    }

    [HttpDelete("{id:guid}"), HasPermission("animals.delete")]
    [Authorize(Roles = "Admin,Administrador")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await writes.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/health-status"), HasPermission("animals.update")]
    public async Task<IActionResult> UpdateHealthStatus(Guid id, LegacyHealthUpdateRequest request, CancellationToken ct)
    {
        await health.UpdateAsync(id, new HealthUpdateRequest(request.Status, request.Reason), ct);
        return Ok(await animals.GetAsync(id, ct));
    }

    [HttpGet]
    [HasPermission("animals.list")]
    public async Task<ActionResult<IReadOnlyList<AnimalListItem>>> List(CancellationToken cancellationToken) =>
        Ok(await animals.ListAsync(cancellationToken));

    [HttpGet("page"), HasPermission("animals.list")]
    public async Task<ActionResult<AnimalPageResult>> Page([FromQuery] AnimalPageRequest request, CancellationToken ct) =>
        Ok(await animals.PageAsync(request, ct));

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
