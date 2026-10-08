using Core.Application.Auditing;
using Core.Application.Livestock;
using Core.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;

namespace Presentation.API.Controllers;

[ApiController, Route("api/admin/auditlogs"), Authorize(Roles = PermissionCatalog.AdminRoles)]
public sealed class AuditLogsController(IAuditReader reader) : ControllerBase
{
    [HttpGet, HasPermission("auditlogs.list")]
    public async Task<ActionResult<CarePage<AuditEntry>>> Page([FromQuery] AuditQuery query, CancellationToken ct) =>
        Ok(await reader.PageAsync(query, ct));

    [HttpGet("options"), HasPermission("auditlogs.list")]
    public async Task<ActionResult<AuditOptions>> Options(CancellationToken ct) => Ok(await reader.OptionsAsync(ct));

    [HttpGet("{id:guid}"), HasPermission("auditlogs.get")]
    public async Task<ActionResult<AuditDetail>> Detail(Guid id, CancellationToken ct) => Ok(await reader.GetAsync(id, ct));
}
