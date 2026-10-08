using Core.Application.Livestock;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Livestock;

public sealed class GrowthMonitoringReader(AppDbContext db) : IGrowthMonitoringReader
{
    public async Task<IReadOnlyList<GrowthObservation>> ReadAsync(IReadOnlyCollection<Guid> farmIds, CancellationToken ct)
    {
        var latest = db.Animals.AsNoTracking().Where(a => farmIds.Contains(a.FarmId) && a.Status == AnimalStatus.Active && a.Species.Code == "BO")
            .Select(a => new {
                a.Id, a.FarmId, a.InternalTag, a.Name, a.TargetDailyGainKg,
                CurrentDate = db.WeightRecords.Where(w => w.AnimalId == a.Id && w.FarmId == a.FarmId).OrderByDescending(w => w.Date).ThenByDescending(w => w.CreatedAt).ThenByDescending(w => w.Id).Select(w => (DateOnly?)w.Date).FirstOrDefault(),
                CurrentWeight = db.WeightRecords.Where(w => w.AnimalId == a.Id && w.FarmId == a.FarmId).OrderByDescending(w => w.Date).ThenByDescending(w => w.CreatedAt).ThenByDescending(w => w.Id).Select(w => (decimal?)w.WeightKg).FirstOrDefault()
            });
        return await latest.Select(a => new GrowthObservation(a.Id, a.FarmId, a.InternalTag, a.Name, a.TargetDailyGainKg, a.CurrentDate, a.CurrentWeight,
            db.WeightRecords.Where(w => w.AnimalId == a.Id && w.FarmId == a.FarmId && w.Date < a.CurrentDate).OrderByDescending(w => w.Date).ThenByDescending(w => w.CreatedAt).ThenByDescending(w => w.Id).Select(w => (DateOnly?)w.Date).FirstOrDefault(),
            db.WeightRecords.Where(w => w.AnimalId == a.Id && w.FarmId == a.FarmId && w.Date < a.CurrentDate).OrderByDescending(w => w.Date).ThenByDescending(w => w.CreatedAt).ThenByDescending(w => w.Id).Select(w => (decimal?)w.WeightKg).FirstOrDefault())).ToListAsync(ct);
    }
}
