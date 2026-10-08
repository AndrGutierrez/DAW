using Core.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;
namespace Presentation.API.Controllers;
[ApiController, Route("api/exchange-rates"), Authorize(Roles = "Admin,Administrador")]
public sealed class ExchangeRatesController(ExchangeRateService service) : ControllerBase
{
    [HttpGet("usd-ves"), HasPermission("products.list")]
    public async Task<IActionResult> Get([FromQuery] DateOnly? date, CancellationToken ct) =>
        Ok(await service.GetAsync(date ?? DateOnly.FromDateTime(DateTime.UtcNow), ct));
    [HttpPost("usd-ves"), HasPermission("products.update")]
    public async Task<IActionResult> Record(ExchangeRateRequest request, CancellationToken ct) =>
        Ok(await service.RecordAsync(request, ct));
}
