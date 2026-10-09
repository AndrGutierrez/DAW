using Core.Application.Livestock;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;

namespace Presentation.API.Controllers;

[ApiController]
public sealed class GrowthMonitoringController(GrowthMonitoringService service) : ControllerBase
{
    [HttpGet("api/farms/{id:guid}/growth-policy"), HasPermission("farms.get")]
    public async Task<IActionResult> Policy(Guid id, CancellationToken ct) => Ok(await service.PolicyAsync(id, ct));

    [HttpPut("api/farms/{id:guid}/growth-policy"), HasPermission("farms.update"), Authorize(Roles = "Admin,Administrador")]
    public async Task<IActionResult> SetPolicy(Guid id, GrowthGoalRequest request, CancellationToken ct) => Ok(await service.SetPolicyAsync(id, request, ct));

    [HttpPut("api/animals/{id:guid}/growth-goal"), HasPermission("animals.update")]
    public async Task<IActionResult> SetGoal(Guid id, GrowthGoalRequest request, CancellationToken ct) => Ok(await service.SetAnimalGoalAsync(id, request, ct));

    [HttpGet("api/alerts/growth"), HasPermission("animals.list"), HasPermission("weights.list")]
    public async Task<IActionResult> Alerts([FromQuery] Guid? farmId, [FromQuery] CarePageRequest query, CancellationToken ct) => Ok(await service.AlertsAsync(farmId, query, ct));
}
