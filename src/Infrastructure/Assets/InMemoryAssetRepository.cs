using Core.Application.Assets;
using Core.Domain.Assets;

namespace Infrastructure.Assets;

public sealed class InMemoryAssetRepository : IAssetRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, AssetCategory> _categories = [];
    private readonly Dictionary<Guid, ITAsset> _assets = [];

    public bool TryAddCategory(AssetCategory category)
    {
        lock (_gate)
        {
            if (_categories.Values.Any(existing =>
                string.Equals(existing.Name, category.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            _categories.Add(category.Id, category);
            return true;
        }
    }

    public AssetCategory? FindCategory(Guid id)
    {
        lock (_gate)
        {
            return _categories.GetValueOrDefault(id);
        }
    }

    public IReadOnlyList<AssetCategory> ListCategories()
    {
        lock (_gate)
        {
            return _categories.Values.OrderBy(category => category.Name).ToArray();
        }
    }

    public bool TryAddAsset(ITAsset asset)
    {
        lock (_gate)
        {
            if (_assets.Values.Any(existing =>
                string.Equals(existing.AssetTag, asset.AssetTag, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            _assets.Add(asset.Id, asset);
            return true;
        }
    }

    public ITAsset? FindAsset(Guid id)
    {
        lock (_gate)
        {
            return _assets.GetValueOrDefault(id);
        }
    }

    public IReadOnlyList<ITAsset> ListAssets()
    {
        lock (_gate)
        {
            return _assets.Values.OrderBy(asset => asset.AssetTag).ToArray();
        }
    }
}
