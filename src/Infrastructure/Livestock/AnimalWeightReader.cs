using Core.Application.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Livestock;

public sealed class AnimalWeightReader(AppDbContext db) : IAnimalWeightReader
{
    public async Task<LatestWeight?> GetLatestAsync(Guid animalId, CancellationToken cancellationToken = default) =>
        await db.WeightRecords
            .AsNoTracking()
            .Where(record => record.AnimalId == animalId)
            .OrderByDescending(record => record.Date)
            .Select(record => new LatestWeight(record.WeightKg, record.BodyConditionScore, record.Date))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, LatestWeight>> GetLatestForAsync(
        IReadOnlyCollection<Guid> animalIds,
        CancellationToken cancellationToken = default)
    {
        if (animalIds.Count == 0)
        {
            return new Dictionary<Guid, LatestWeight>();
        }

        var records = await db.WeightRecords
            .AsNoTracking()
            .Where(record => animalIds.Contains(record.AnimalId))
            .Select(record => new
            {
                record.AnimalId,
                record.WeightKg,
                record.BodyConditionScore,
                record.Date
            })
            .ToListAsync(cancellationToken);

        return records
            .GroupBy(record => record.AnimalId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var latest = group.MaxBy(record => record.Date)!;
                    return new LatestWeight(latest.WeightKg, latest.BodyConditionScore, latest.Date);
                });
    }
}
