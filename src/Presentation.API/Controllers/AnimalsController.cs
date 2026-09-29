using Core.Application.Cattle;
using Core.Domain.Cattle;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API.Controllers;

[ApiController]
[Route("api/animals")]
public sealed class AnimalsController(ICattleCatalogService catalog) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<AnimalResult>> List() =>
        Ok(catalog.ListAnimals());

    [HttpGet("{id:guid}")]
    public ActionResult<AnimalResult> Get(Guid id) =>
        Ok(catalog.GetAnimal(id));

    [HttpPost]
    public ActionResult<AnimalResult> Create(CreateAnimalRequest request)
    {
        var animal = catalog.RegisterAnimal(
            request.EarTag,
            request.Breed,
            request.HerdId,
            request.DateOfBirth);

        return CreatedAtAction(nameof(Get), new { id = animal.Id }, animal);
    }

    [HttpPatch("{id:guid}/health-status")]
    public ActionResult<AnimalResult> UpdateHealthStatus(Guid id, UpdateHealthStatusRequest request) =>
        Ok(catalog.UpdateAnimalHealthStatus(id, request.Status));
}

public sealed record CreateAnimalRequest(
    string EarTag,
    string Breed,
    Guid HerdId,
    DateOnly? DateOfBirth);

public sealed record UpdateHealthStatusRequest(AnimalHealthStatus Status);
