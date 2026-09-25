namespace Core.Application.Assets;

public sealed class AssetTagNormalizer : IAssetTagNormalizer
{
    public string Normalize(string assetTag)
    {
        if (string.IsNullOrWhiteSpace(assetTag))
        {
            throw new ArgumentException("An asset tag is required.", nameof(assetTag));
        }

        return assetTag.Trim().ToUpperInvariant();
    }
}
