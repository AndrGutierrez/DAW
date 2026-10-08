using Core.Application.Livestock;
using Core.Application.Management;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Livestock;

public sealed class PaddockReader(AppDbContext db) : IPaddockReader
{
    private IQueryable<Arrival> Arrivals(Guid[] paddocks) => db.Animals.AsNoTracking()
        .Where(a => a.Status == AnimalStatus.Active && a.PaddockId != null && paddocks.Contains(a.PaddockId.Value) && a.Paddock!.FarmId == a.FarmId)
        .Select(a => new Arrival {
            PaddockId = a.PaddockId!.Value, AnimalId = a.Id, InternalTag = a.InternalTag, Name = a.Name,
            Lot = a.Lot == null ? null : a.Lot.Name, LotId = a.LotId, SpeciesId = a.SpeciesId,
            Date = db.AnimalMovements.Where(m => m.AnimalId == a.Id && m.FarmId == a.FarmId && m.FromPaddockId != m.ToPaddockId)
                .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
                .Select(m => m.ToPaddockId == a.PaddockId ? (DateOnly?)m.Date : null).FirstOrDefault()
        });
    public async Task<CarePage<PaddockSnapshot>> PageAsync(PaddockPageRequest request, IReadOnlyCollection<Guid> farmIds, CancellationToken ct)
    {
        var query = db.Paddocks.AsNoTracking().Where(p => farmIds.Contains(p.FarmId));
        return await ReadPageAsync(request, query, ct);
    }
    public async Task<IReadOnlyList<PaddockSnapshot>> MapAsync(Guid farmId, CancellationToken ct)
    {
        var query = db.Paddocks.AsNoTracking().Where(p => p.FarmId == farmId && p.IsActive && p.MapX != null);
        var count = await query.CountAsync(ct);
        return (await ReadPageAsync(new PaddockPageRequest(PageSize: Math.Max(1, count)), query, ct)).Items;
    }
    private async Task<CarePage<PaddockSnapshot>> ReadPageAsync(PaddockPageRequest request, IQueryable<Paddock> query, CancellationToken ct)
    {
        if (request.FarmId is Guid farm) query = query.Where(p => p.FarmId == farm);
        if (request.IsActive is bool active) query = query.Where(p => p.IsActive == active);
        var search = request.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(search)) query = query.Where(p => p.Name.ToLower().Contains(search) || (p.Code != null && p.Code.ToLower().Contains(search)));
        var total = await query.CountAsync(ct);
        var page = await query.OrderBy(p => p.Name).ThenBy(p => p.Id).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(p => new { p.Id, p.FarmId, p.Name, p.Code, p.AreaHectares, p.Capacity, p.IsActive, p.MaxStayDays, p.MapX, p.MapY, p.MapWidth, p.MapHeight, Farm = p.Farm.Name }).ToListAsync(ct);
        var ids = page.Select(p => p.Id).ToArray();
        var occupancy = await Arrivals(ids).GroupBy(a => a.PaddockId)
            .Select(g => new { Id = g.Key, Count = g.Count(), Oldest = g.Min(a => a.Date), Unknown = g.Count(a => a.Date == null) }).ToDictionaryAsync(g => g.Id, ct);
        var lots = await Arrivals(ids).GroupBy(a => new { a.PaddockId, a.LotId, a.Lot })
            .Select(g => new { g.Key.PaddockId, g.Key.LotId, g.Key.Lot, Count = g.Count() }).ToListAsync(ct);
        return new(page.Select(p => {
            occupancy.TryGetValue(p.Id, out var summary);
            return new PaddockSnapshot(p.Id, new PaddockRequest(p.FarmId, p.Name, p.Code, p.AreaHectares, p.Capacity, p.IsActive, p.MaxStayDays, p.MapX, p.MapY, p.MapWidth, p.MapHeight), p.Farm,
                summary?.Count ?? 0, summary?.Oldest, summary?.Unknown ?? 0,
                lots.Where(l => l.PaddockId == p.Id).OrderBy(l => l.Lot).Select(l => new PaddockLotCount(l.LotId, l.Lot, l.Count)).ToList());
        }).ToList(), total, request.Page, request.PageSize);
    }
    public async Task<CarePage<PaddockResident>> ResidentsAsync(Guid paddockId, PaddockResidentPageRequest query, CancellationToken ct)
    {
        var animals = Arrivals([paddockId]);
        if (query.LotId is Guid lot) animals = animals.Where(a => a.LotId == lot);
        if (query.Ungrouped) animals = animals.Where(a => a.LotId == null);
        var total = await animals.CountAsync(ct);
        var items = await animals.OrderBy(a => a.InternalTag).ThenBy(a => a.AnimalId).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(a => new PaddockResident(a.AnimalId, a.InternalTag, a.Name, a.Lot, a.LotId, a.SpeciesId, a.Date)).ToListAsync(ct);
        return new(items, total, query.Page, query.PageSize);
    }
    private sealed class Arrival
    {
        public Guid PaddockId { get; init; }
        public Guid AnimalId { get; init; }
        public string InternalTag { get; init; } = "";
        public string? Name { get; init; }
        public string? Lot { get; init; }
        public Guid? LotId { get; init; }
        public Guid SpeciesId { get; init; }
        public DateOnly? Date { get; init; }
    }
}
