using Core.Application.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;

namespace Presentation.API.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ICrudService<ProductRequest> service) : ControllerBase
{
    [HttpGet, HasPermission("products.list")]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.ListAsync(ct));

    [HttpGet("{id:guid}"), HasPermission("products.get")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await service.GetAsync(id, ct));

    [HttpPost, HasPermission("products.create")]
    [Authorize(Roles = "Admin,Administrador")]
    public async Task<IActionResult> Create(ProductRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}"), HasPermission("products.update")]
    [Authorize(Roles = "Admin,Administrador")]
    public async Task<IActionResult> Update(Guid id, ProductRequest request, CancellationToken ct) => Ok(await service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}"), HasPermission("products.delete")]
    [Authorize(Roles = "Admin,Administrador")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
