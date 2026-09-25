using Core.Domain.Assets;

namespace Core.Application.Assets;

public sealed class AssetCatalogService(
    IAssetRepository repository,
    IAssetTagNormalizer tagNormalizer) : IAssetCatalogService
{
    public CategoryResult CreateCategory(string name, string? description)
    {
        var category = new AssetCategory(name, description);

        if (!repository.TryAddCategory(category))
        {
            throw new InvalidOperationException("A category with this name already exists.");
        }

        return ToResult(category);
    }

    public IReadOnlyList<CategoryResult> ListCategories() =>
        repository.ListCategories().Select(ToResult).ToArray();

    public CategoryResult GetCategory(Guid id)
    {
        var category = repository.FindCategory(id)
            ?? throw new KeyNotFoundException("The requested asset category was not found.");

        return ToResult(category);
    }

    public AssetResult RegisterAsset(
        string assetTag,
        string name,
        Guid categoryId,
        string? serialNumber)
    {
        var category = repository.FindCategory(categoryId)
            ?? throw new KeyNotFoundException("The requested asset category was not found.");

        var asset = new ITAsset(tagNormalizer.Normalize(assetTag), name, category, serialNumber);

        if (!repository.TryAddAsset(asset))
        {
            throw new InvalidOperationException("An asset with this tag already exists.");
        }

        return ToResult(asset);
    }

    public AssetResult GetAsset(Guid id)
    {
        var asset = repository.FindAsset(id)
            ?? throw new KeyNotFoundException("The requested asset was not found.");

        return ToResult(asset);
    }

    public IReadOnlyList<AssetResult> ListAssets() =>
        repository.ListAssets().Select(ToResult).ToArray();

    private static CategoryResult ToResult(AssetCategory category) =>
        new(category.Id, category.Name, category.Description, category.CreatedAt);

    private static AssetResult ToResult(ITAsset asset) =>
        new(
            asset.Id,
            asset.AssetTag,
            asset.Name,
            asset.SerialNumber,
            asset.Status,
            asset.CategoryId,
            asset.Category.Name,
            asset.CreatedAt);
}
