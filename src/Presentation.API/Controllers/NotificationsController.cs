using Core.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Presentation.API.Controllers;

[ApiController, Route("api/notifications"), Authorize]
public sealed class NotificationsController(NotificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(await service.GetAsync(ct));
    }
}
