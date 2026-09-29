namespace Core.Application.Cattle;

public sealed class AnimalTagNormalizer : IAnimalTagNormalizer
{
    public string Normalize(string earTag)
    {
        if (string.IsNullOrWhiteSpace(earTag))
        {
            throw new ArgumentException("An ear tag is required.", nameof(earTag));
        }

        return earTag.Trim().ToUpperInvariant();
    }
}
