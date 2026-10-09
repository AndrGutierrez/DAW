using Core.Application.Livestock;
using Core.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;
namespace Presentation.API.Controllers;

[ApiController,Route("api/admin/users"),Authorize(Roles=PermissionCatalog.AdminRoles)]
public sealed class UsersController(UserManagementService users) : ControllerBase
{
    [HttpGet,HasPermission("users.list")]
    public async Task<ActionResult<CarePage<ManagedUser>>> Page([FromQuery]UserPageQuery q,CancellationToken ct)=>Ok(await users.PageAsync(q,ct));
    [HttpPost,HasPermission("users.create"),HasPermission("roles.manage")]
    public async Task<IActionResult> Create(UserWriteRequest q,CancellationToken ct){var result=await users.CreateAsync(q,ct);return StatusCode(201,result);}
    [HttpPut("{id:guid}"),HasPermission("users.update"),HasPermission("roles.manage")]
    public async Task<ActionResult<ManagedUser>> Update(Guid id,UserWriteRequest q,CancellationToken ct)=>Ok(await users.UpdateAsync(id,q,ct));
    [HttpPost("{id:guid}/password"),HasPermission("users.update")]
    public async Task<IActionResult> Password(Guid id,PasswordResetRequest q,CancellationToken ct){await users.ResetPasswordAsync(id,q,ct);return NoContent();}
}
