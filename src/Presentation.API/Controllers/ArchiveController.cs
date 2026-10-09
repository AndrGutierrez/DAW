using Core.Application.Management;
using Core.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;
namespace Presentation.API.Controllers;
[ApiController, Route("api/admin/archive"), Authorize(Roles = PermissionCatalog.AdminRoles)]
public sealed class ArchiveController(IArchiveStore store) : ControllerBase
{
    [HttpGet, HasPermission("archive.list")]
    public async Task<IActionResult> Page([FromQuery] ArchiveQuery query, CancellationToken ct) => Ok(await store.PageAsync(query, ct));
    [HttpGet("{resource}/{id:guid}"), HasPermission("archive.get")]
    public async Task<IActionResult> Detail(string resource, Guid id, CancellationToken ct) => Ok(await store.DetailAsync(resource, id, ct));
    [HttpPost("{resource}/{id:guid}/restore"), HasPermission("archive.restore")]
    public async Task<IActionResult> Restore(string resource, Guid id, CancellationToken ct)
    {
        await store.RestoreAsync(resource, id, ct); return NoContent();
    }
}
