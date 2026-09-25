using Core.Domain.Common;

namespace Core.Domain.Assets;

public sealed class AssetCategory : BaseEntity
{
    public AssetCategory(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A category name is required.", nameof(name));
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public string Name { get; }

    public string? Description { get; }
}
