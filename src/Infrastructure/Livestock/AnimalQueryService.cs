using Core.Application.Livestock;
using Core.Application.Security;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Livestock;

public sealed class AnimalQueryService(AppDbContext db, IAnimalWeightReader weights, IFarmAccess farmAccess) : IAnimalQueryService
{
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
                animal.SpeciesId))
            .ToList();
    }
}
