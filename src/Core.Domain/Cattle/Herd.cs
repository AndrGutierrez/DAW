using Core.Domain.Common;

namespace Core.Domain.Cattle;

public sealed class Herd : BaseEntity
{
    public Herd(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A herd name is required.", nameof(name));
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public string Name { get; }
    public string? Description { get; }
}
