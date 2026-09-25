namespace Core.Application.Assets;

public interface IAssetCatalogService
{
    CategoryResult CreateCategory(string name, string? description);

    CategoryResult GetCategory(Guid id);

    IReadOnlyList<CategoryResult> ListCategories();

    AssetResult RegisterAsset(string assetTag, string name, Guid categoryId, string? serialNumber);

    AssetResult GetAsset(Guid id);

    IReadOnlyList<AssetResult> ListAssets();
}
