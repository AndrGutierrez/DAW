using Core.Application.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.Livestock;

public sealed class AnimalIdentificationReader(AppDbContext db) : IAnimalIdentificationReader
{
    public async Task<IReadOnlyList<Guid>> FindAsync(Guid farmId, string identifier, CancellationToken ct)
    {
        var hasId = Guid.TryParse(identifier, out var id);
        var normalized = identifier.ToUpperInvariant();
        return await db.Animals.AsNoTracking().Where(a => a.FarmId == farmId &&
            (hasId && a.Id == id || a.InternalTag == normalized || a.OfficialId == normalized || a.Rfid == normalized))
            .OrderBy(a => a.Id).Select(a => a.Id).Take(2).ToListAsync(ct);
    }
}
