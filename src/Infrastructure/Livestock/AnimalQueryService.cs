using Core.Application.Livestock;
using FluentValidation;
using Core.Application.Security;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Livestock;

public sealed class AnimalQueryService(AppDbContext db, IAnimalWeightReader weights, IFarmAccess farmAccess) : IAnimalQueryService
{
    public async Task<AnimalPageResult> PageAsync(AnimalPageRequest request, CancellationToken cancellationToken = default)
    {
        await new AnimalPageRequestValidator().ValidateAndThrowAsync(request, cancellationToken);
        var farmIds = await farmAccess.GetAccessibleFarmIdsAsync(cancellationToken);
        var query = db.Animals.AsNoTracking().Where(animal => farmIds.Contains(animal.FarmId));
        if (request.FarmId.HasValue) query = query.Where(animal => animal.FarmId == request.FarmId.Value);
        if (request.SpeciesId.HasValue) query = query.Where(animal => animal.SpeciesId == request.SpeciesId.Value);
        if (request.Sex.HasValue) query = query.Where(animal => animal.Sex == request.Sex.Value);
        if (request.ExcludeId.HasValue) query = query.Where(animal => animal.Id != request.ExcludeId.Value);
        if (request.DamId.HasValue) query = query.Where(animal => animal.DamId == request.DamId.Value);
        if (request.LotId.HasValue) query = query.Where(animal => animal.LotId == request.LotId.Value);
        if (request.Status.HasValue) query = query.Where(animal => animal.Status == request.Status.Value);
        var search = request.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(animal => animal.InternalTag.ToLower().Contains(search) ||
                (animal.Name != null && animal.Name.ToLower().Contains(search)) ||
                (animal.OfficialId != null && animal.OfficialId.ToLower().Contains(search)));
        var total = await query.CountAsync(cancellationToken);
        var items = await MaterializeAsync(query.OrderBy(animal => animal.InternalTag).ThenBy(animal => animal.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize), cancellationToken);
        return new AnimalPageResult(items, total, request.Page, request.PageSize);
    }

    public async Task<IReadOnlyList<AnimalListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        var farmIds = await farmAccess.GetAccessibleFarmIdsAsync(cancellationToken);
        return await MaterializeAsync(
            db.Animals.AsNoTracking().Where(animal => farmIds.Contains(animal.FarmId))
                .OrderBy(animal => animal.InternalTag), cancellationToken);
    }

    public async Task<IReadOnlyList<AnimalListItem>> ListStaleAsync(int days, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var farmIds = await farmAccess.GetAccessibleFarmIdsAsync(cancellationToken);

        return await MaterializeAsync(
            db.Animals.AsNoTracking()
                .Where(animal => farmIds.Contains(animal.FarmId) && animal.UpdatedAt < cutoff)
                .OrderBy(animal => animal.UpdatedAt),
            cancellationToken);
    }

    public async Task<AnimalDetail> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var farmIds = await farmAccess.GetAccessibleFarmIdsAsync(cancellationToken);
        var animal = await db.Animals
            .AsNoTracking()
            .Include(candidate => candidate.Species)
            .Include(candidate => candidate.Breed)
            .Include(candidate => candidate.Lot)
            .Include(candidate => candidate.Paddock)
            .Include(candidate => candidate.Farm)
            .Include(candidate => candidate.Photos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(candidate => candidate.Id == id && farmIds.Contains(candidate.FarmId), cancellationToken)
            ?? throw new KeyNotFoundException("The requested animal was not found.");

        var latest = await weights.GetLatestAsync(id, cancellationToken);

        var photos = animal.Photos
            .OrderByDescending(photo => photo.UploadedAt)
            .Select(photo => new AnimalPhotoInfo(photo.Id, photo.Url, photo.UploadedAt))
            .ToList();

        return new AnimalDetail(
            animal.Id,
            animal.InternalTag,
            animal.OfficialId,
            animal.Rfid,
            animal.Name,
            animal.Species.Name,
            animal.Breed?.Name,
            animal.Sex.ToString(),
            animal.Status.ToString(),
            animal.HealthStatus.ToString(),
            animal.Origin.ToString(),
            animal.Purpose.ToString(),
            animal.BirthDate,
            animal.BirthWeightKg,
            latest?.WeightKg,
            latest?.BodyConditionScore,
            animal.Color,
            animal.Markings,
            animal.Lot?.Name,
            animal.Paddock?.Name,
            animal.Farm.Name,
            photos,
            animal.UpdatedAt,
            animal.Notes,
            animal.FarmId,
            animal.SpeciesId,
            animal.BreedId,
            animal.LotId,
            animal.PaddockId,
            animal.DamId,
            animal.SireId);
    }

    public async Task<AnimalLineage> GetLineageAsync(Guid id, CancellationToken ct = default)
    {
        var farms = await farmAccess.GetAccessibleFarmIdsAsync(ct);
        return await db.Animals.IgnoreQueryFilters().AsNoTracking().Where(a => a.Id == id && farms.Contains(a.FarmId))
            .Select(a => new AnimalLineage(a.Id, a.InternalTag, a.Name, a.DamId, a.SireId, a.IsDeleted)).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("The ancestry record was not found.");
    }

    private async Task<IReadOnlyList<AnimalListItem>> MaterializeAsync(
        IQueryable<Animal> query,
        CancellationToken cancellationToken)
    {
        var animals = await query
            .Include(animal => animal.Species)
            .Include(animal => animal.Breed)
            .Include(animal => animal.Lot)
            .Include(animal => animal.Farm)
            .Include(animal => animal.Photos)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var latestWeights = await weights.GetLatestForAsync(
            animals.Select(animal => animal.Id).ToArray(),
            cancellationToken);

        return animals
            .Select(animal => new AnimalListItem(
                animal.Id,
                animal.InternalTag,
                animal.Name,
                animal.Species.Name,
                animal.Breed?.Name,
                animal.Sex.ToString(),
                animal.Status.ToString(),
                animal.HealthStatus.ToString(),
                animal.BirthDate,
                latestWeights.TryGetValue(animal.Id, out var latest) ? latest.WeightKg : null,
                animal.Lot?.Name,
                animal.Farm.Name,
                animal.Photos
                    .OrderByDescending(photo => photo.UploadedAt)
                    .Select(photo => photo.Url)
                    .FirstOrDefault(),
                animal.Photos.Count,
                animal.UpdatedAt,
                animal.FarmId,
                animal.SpeciesId,
                animal.LotId,
                animal.PaddockId))
            .ToList();
    }
}
