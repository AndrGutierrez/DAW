using Core.Application.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;

namespace Presentation.API.Controllers;

[ApiController]
[Route("api/production")]
public sealed class ProductionController(ICrudService<ProductionRequest> service) : ControllerBase
{
    [HttpGet, HasPermission("production.list")]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.ListAsync(ct));

    [HttpGet("{id:guid}"), HasPermission("production.get")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await service.GetAsync(id, ct));

    [HttpPost, HasPermission("production.create")]
    public async Task<IActionResult> Create(ProductionRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}"), HasPermission("production.update")]
    public async Task<IActionResult> Update(Guid id, ProductionRequest request, CancellationToken ct) => Ok(await service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}"), HasPermission("production.delete")]
    [Authorize(Roles = "Admin,Administrador")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
