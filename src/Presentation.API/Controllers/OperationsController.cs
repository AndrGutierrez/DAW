using Core.Application.Livestock;
using Core.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;
namespace Presentation.API.Controllers;

[ApiController, Route("api/inventory")]
public sealed class InventoryOperationsController(InventoryService service) : ControllerBase
{
    [HttpGet("page"), HasPermission("inventory.list"), HasPermission("products.list")]
    public async Task<IActionResult> Page([FromQuery] InventoryQuery q, CancellationToken ct) => Ok(await service.PageAsync(q, ct));
    [HttpGet("{id:guid}/movements"), HasPermission("inventory.get")]
    public async Task<IActionResult> History(Guid id, [FromQuery] CarePageRequest q, CancellationToken ct) => Ok(await service.HistoryAsync(id, q, ct));
    [HttpPost("{id:guid}/movements"), HasPermission("inventory.update"), HasPermission("inventory.get")]
    public async Task<IActionResult> Record(Guid id, StockRequest q, CancellationToken ct) { var result = await service.RecordAsync(id, q, ct); return StatusCode(result.Replayed ? 200 : 201, result); }
    [HttpGet("products/page"), HasPermission("products.list")]
    public async Task<IActionResult> Products([FromQuery] ProductQuery q, CancellationToken ct) => Ok(await service.ProductsAsync(q, ct));
}
[ApiController, Route("api/analytics"), Authorize(Roles = "Admin,Administrador")]
public sealed class AnalyticsController(AnalyticsService service) : ControllerBase
{
    [HttpGet("overview"), HasPermission("inventory.list"), HasPermission("products.list"), HasPermission("production.list"), HasPermission("weights.list"), HasPermission("reproduction.list"), HasPermission("animals.list")]
    public async Task<IActionResult> Get([FromQuery] PeriodQuery q, CancellationToken ct) => Ok(await service.GetAsync(q, ct));
}
[ApiController, Route("api/reports")]
public sealed class ReportsController(ReportService service) : ControllerBase
{
    [HttpGet("clinical/export"), HasPermission("clinical.list"), HasPermission("animals.list")]
    public async Task<IActionResult> ExportClinical([FromQuery] PeriodQuery q, CancellationToken ct) => Ok(await service.ExportAsync(q, true, ct));
    [HttpGet("production/export"), HasPermission("production.list"), HasPermission("animals.list")]
    public async Task<IActionResult> ExportProduction([FromQuery] PeriodQuery q, CancellationToken ct) => Ok(await service.ExportAsync(q, false, ct));
    [HttpGet("clinical"), HasPermission("clinical.list"), HasPermission("animals.list")]
    public async Task<IActionResult> Clinical([FromQuery] ReportQuery q, CancellationToken ct) => Ok(await service.GetAsync(q, true, ct));
    [HttpGet("production"), HasPermission("production.list"), HasPermission("animals.list")]
    public async Task<IActionResult> Production([FromQuery] ReportQuery q, CancellationToken ct) => Ok(await service.GetAsync(q, false, ct));
}
