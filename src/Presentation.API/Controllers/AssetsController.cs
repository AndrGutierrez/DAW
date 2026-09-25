using Core.Application.Assets;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API.Controllers;

[ApiController]
[Route("api/assets")]
public sealed class AssetsController(IAssetCatalogService catalog) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<AssetResult>> List() =>
        Ok(catalog.ListAssets());

    [HttpGet("{id:guid}")]
    public ActionResult<AssetResult> Get(Guid id) =>
        Ok(catalog.GetAsset(id));

    [HttpPost]
    public ActionResult<AssetResult> Create(CreateAssetRequest request)
    {
        var asset = catalog.RegisterAsset(
            request.AssetTag,
            request.Name,
            request.CategoryId,
            request.SerialNumber);

        return CreatedAtAction(nameof(Get), new { id = asset.Id }, asset);
    }
}

public sealed record CreateAssetRequest(
    string AssetTag,
    string Name,
    Guid CategoryId,
    string? SerialNumber);
