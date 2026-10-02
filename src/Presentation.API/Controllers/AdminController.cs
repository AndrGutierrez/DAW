using Core.Application.Security;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;

namespace Presentation.API.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController(IAccessAdministration access) : ControllerBase
{
    [HttpGet("permissions")]
    [HasPermission("permissions.list")]
    public async Task<ActionResult<IReadOnlyList<PermissionResult>>> ListPermissions(CancellationToken cancellationToken) =>
        Ok(await access.ListPermissionsAsync(cancellationToken));

    [HttpGet("roles")]
    [HasPermission("roles.list")]
    public async Task<ActionResult<IReadOnlyList<RoleResult>>> ListRoles(CancellationToken cancellationToken) =>
        Ok(await access.ListRolesAsync(cancellationToken));

    [HttpPost("roles/{roleId:guid}/permissions/{permissionId:guid}")]
    [HasPermission("roles.list")]
    public async Task<ActionResult<RoleResult>> GrantPermission(
        Guid roleId,
        Guid permissionId,
        CancellationToken cancellationToken) =>
        Ok(await access.GrantPermissionAsync(roleId, permissionId, cancellationToken));

    [HttpDelete("roles/{roleId:guid}/permissions/{permissionId:guid}")]
    [HasPermission("roles.list")]
    public async Task<ActionResult<RoleResult>> RevokePermission(
        Guid roleId,
        Guid permissionId,
        CancellationToken cancellationToken) =>
        Ok(await access.RevokePermissionAsync(roleId, permissionId, cancellationToken));
}
