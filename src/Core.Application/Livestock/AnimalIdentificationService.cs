using Core.Application.Security;
namespace Core.Application.Livestock;

public interface IAnimalIdentificationReader
{
    Task<IReadOnlyList<Guid>> FindAsync(Guid farmId, string identifier, CancellationToken ct);
}
public sealed class AnimalIdentificationService(IFarmAccess farms, IAnimalIdentificationReader reader, IAnimalQueryService animals)
{
    public async Task<AnimalDetail> IdentifyAsync(Guid farmId, string identifier, CancellationToken ct = default)
    {
        if (farmId == Guid.Empty || string.IsNullOrWhiteSpace(identifier) || identifier.Length > 160)
            throw new ArgumentException("A farm and an identifier of at most 160 characters are required.");
        if (!await farms.CanAccessAsync(farmId, ct)) throw new KeyNotFoundException("The requested animal was not found.");
        var normalized = identifier.Trim().ToUpperInvariant();
        if (normalized.StartsWith("DAW:ANIMAL:"))
        {
            if (!Guid.TryParse(normalized[11..], out var id)) throw new ArgumentException("The animal label is invalid.");
            normalized = id.ToString();
        }
        var matches = await reader.FindAsync(farmId, normalized, ct);
        if (matches.Count == 0) throw new KeyNotFoundException("The requested animal was not found.");
        if (matches.Count > 1) throw new Core.Application.Management.ConflictException("The identifier matches more than one animal. Use its unique animal label.");
        return await animals.GetAsync(matches[0], ct);
    }
}
