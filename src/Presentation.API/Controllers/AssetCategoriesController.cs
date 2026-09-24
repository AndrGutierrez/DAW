using Core.Application.Assets;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class AssetCategoriesController(IAssetCatalogService catalog) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<CategoryResult>> List() =>
        Ok(catalog.ListCategories());

    [HttpGet("{id:guid}")]
    public ActionResult<CategoryResult> Get(Guid id) =>
        Ok(catalog.GetCategory(id));

    [HttpPost]
    public ActionResult<CategoryResult> Create(CreateCategoryRequest request)
    {
        var category = catalog.CreateCategory(request.Name, request.Description);
        return CreatedAtAction(nameof(Get), new { id = category.Id }, category);
    }
}

public sealed record CreateCategoryRequest(string Name, string? Description);
