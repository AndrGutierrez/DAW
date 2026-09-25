using Core.Domain.Assets;

namespace Core.Application.Assets;

public interface IAssetRepository
{
    bool TryAddCategory(AssetCategory category);

    AssetCategory? FindCategory(Guid id);

    IReadOnlyList<AssetCategory> ListCategories();

    bool TryAddAsset(ITAsset asset);

    ITAsset? FindAsset(Guid id);

    IReadOnlyList<ITAsset> ListAssets();
}
