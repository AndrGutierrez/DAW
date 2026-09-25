using Core.Domain.Common;

namespace Core.Domain.Assets;

public sealed class ITAsset : BaseEntity
{
    public ITAsset(
        string assetTag,
        string name,
        AssetCategory category,
        string? serialNumber = null)
    {
        if (string.IsNullOrWhiteSpace(assetTag))
        {
            throw new ArgumentException("An asset tag is required.", nameof(assetTag));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An asset name is required.", nameof(name));
        }

        Category = category ?? throw new ArgumentNullException(nameof(category));
        AssetTag = assetTag.Trim();
        Name = name.Trim();
        SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim();
        Status = AssetStatus.InStock;
    }

    public string AssetTag { get; }

    public string Name { get; }

    public string? SerialNumber { get; }

    public AssetStatus Status { get; private set; }

    public AssetCategory Category { get; }

    public Guid CategoryId => Category.Id;

    public void ChangeStatus(AssetStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        Status = status;
    }
}
