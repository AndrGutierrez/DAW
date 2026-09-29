using Core.Application.Cattle;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API.Controllers;

[ApiController]
[Route("api/herds")]
public sealed class HerdsController(ICattleCatalogService catalog) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<HerdResult>> List() =>
        Ok(catalog.ListHerds());

    [HttpGet("{id:guid}")]
    public ActionResult<HerdResult> Get(Guid id) =>
        Ok(catalog.GetHerd(id));

    [HttpPost]
    public ActionResult<HerdResult> Create(CreateHerdRequest request)
    {
        var herd = catalog.CreateHerd(request.Name, request.Description);
        return CreatedAtAction(nameof(Get), new { id = herd.Id }, herd);
    }
}

public sealed record CreateHerdRequest(string Name, string? Description);
