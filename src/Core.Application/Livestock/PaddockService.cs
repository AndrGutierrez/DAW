using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;

namespace Core.Application.Livestock;

public sealed class PaddockService(IManagementRepository repository, IFarmAccess farms, IPaddockReader reader)
{
    public async Task<IReadOnlyList<PaddockSnapshot>> MapAsync(Guid farmId, CancellationToken ct = default)
    {
        if (!await farms.CanAccessAsync(farmId, ct) || await repository.GetAsync<Farm>(farmId, ct: ct) == null)
            throw new KeyNotFoundException("The farm was not found.");
        return await reader.MapAsync(farmId, ct);
    }
    public async Task<IReadOnlyList<PaddockSnapshot>> DestinationsAsync(Guid farmId, CancellationToken ct = default)
    {
        if (!await farms.CanAccessAsync(farmId, ct) || await repository.GetAsync<Farm>(farmId, ct: ct) == null)
            throw new KeyNotFoundException("The farm was not found.");
        return await reader.DestinationsAsync(farmId, ct);
    }
    public async Task<CarePage<PaddockSnapshot>> PageAsync(PaddockPageRequest query, CancellationToken ct = default)
    {
        await new PaddockPageRequestValidator().ValidateAndThrowAsync(query, ct);
        return await reader.PageAsync(query, await farms.GetAccessibleFarmIdsAsync(ct), ct);
    }
    public async Task<CarePage<PaddockResident>> ResidentsAsync(Guid id, PaddockResidentPageRequest query, CancellationToken ct = default)
    {
        await new PaddockResidentPageRequestValidator().ValidateAndThrowAsync(query, ct);
        var paddock = await repository.GetAsync<Paddock>(id, ct: ct) ?? throw new KeyNotFoundException("The requested paddock was not found.");
        if (!await farms.CanAccessAsync(paddock.FarmId, ct)) throw new KeyNotFoundException("The requested paddock was not found.");
        return await reader.ResidentsAsync(id, query, ct);
    }
}
public sealed class AnimalMovementService(IManagementRepository repository, IFarmAccess farms, ICurrentUser user, AnimalLocationPolicy location)
{
    private async Task<Animal> FindAsync(Guid id, bool tracking, CancellationToken ct)
    {
        var animal = await repository.GetAsync<Animal>(id, tracking, ct) ?? throw new KeyNotFoundException("The requested animal was not found.");
        if (!await farms.CanAccessAsync(animal.FarmId, ct)) throw new KeyNotFoundException("The requested animal was not found.");
        return animal;
    }
    public async Task<CarePage<AnimalMovementRecord>> GetAsync(Guid id, CarePageRequest query, CancellationToken ct = default)
    {
        await new CarePageRequestValidator().ValidateAndThrowAsync(query, ct);
        var animal = await FindAsync(id, false, ct);
        var count = await repository.CountAsync<AnimalMovement>(m => m.AnimalId == id && m.FarmId == animal.FarmId, ct);
        var items = await repository.PageAsync<AnimalMovement, DateTime>(m => m.AnimalId == id && m.FarmId == animal.FarmId, m => m.CreatedAt, (query.Page - 1) * query.PageSize, query.PageSize, ct);
        return new(items.Select(Read).ToList(), count, query.Page, query.PageSize);
    }
    public Task<CareSubmission<AnimalMovementRecord>> RecordAsync(Guid id, AnimalMovementRequest query, CancellationToken ct = default) =>
        repository.ExecuteWriteAsync(() => RecordCoreAsync(id, query, ct), ct);

    private async Task<CareSubmission<AnimalMovementRecord>> RecordCoreAsync(Guid id, AnimalMovementRequest query, CancellationToken ct)
    {
        await new AnimalMovementRequestValidator().ValidateAndThrowAsync(query, ct);
        var q = query with { Reason = query.Reason.Trim() };
        var animal = await FindAsync(id, true, ct);
        var existing = await repository.GetAsync<AnimalMovement>(q.SubmissionId, ct: ct);
        if (existing is not null)
        {
            if (existing.AnimalId != id || existing.FarmId != animal.FarmId || existing.UserId != user.UserId ||
                existing.FromPaddockId != q.ExpectedFromPaddockId || existing.FromLotId != q.ExpectedFromLotId ||
                existing.ToPaddockId != q.ToPaddockId || existing.ToLotId != q.ToLotId || existing.Reason != q.Reason)
                throw new ConflictException("This submission identifier belongs to a different movement.");
            return new CareSubmission<AnimalMovementRecord>(existing.Id, true, Read(existing));
        }
        if (animal.Status != AnimalStatus.Active) throw new ConflictException("Only active animals can be moved.");
        var farm = await repository.GetAsync<Farm>(animal.FarmId, ct: ct);
        if (farm is null || !farm.IsActive) throw new ConflictException("The farm is inactive.");
        if (animal.PaddockId != q.ExpectedFromPaddockId || animal.LotId != q.ExpectedFromLotId)
            throw new ConflictException("The animal location changed. Refresh before moving it.");
        if (animal.PaddockId == q.ToPaddockId && animal.LotId == q.ToLotId)
            throw new ConflictException("The animal is already at this location.");
        await location.CheckAsync(animal, q.ToPaddockId, q.ToLotId, ct);
        var movement = new AnimalMovement(q.SubmissionId) { AnimalId = id, FarmId = animal.FarmId,
            FromPaddockId = animal.PaddockId, FromLotId = animal.LotId, ToPaddockId = q.ToPaddockId, ToLotId = q.ToLotId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow), Reason = q.Reason, UserId = user.UserId };
        animal.PaddockId = q.ToPaddockId; animal.LotId = q.ToLotId; animal.UpdatedAt = DateTime.UtcNow;
        repository.Add(movement); await repository.SaveAsync(ct);
        return new CareSubmission<AnimalMovementRecord>(movement.Id, false, Read(movement));
    }

    public Task<AnimalMovementBatchResult> RecordBatchAsync(AnimalMovementBatchRequest query, CancellationToken ct = default) =>
        repository.ExecuteWriteAsync(async () =>
        {
            await new AnimalMovementBatchRequestValidator().ValidateAndThrowAsync(query, ct);
            // Check every member before writing; the outer serializable transaction also protects capacity.
            foreach (var item in query.Animals)
            {
                var animal = await FindAsync(item.AnimalId, false, ct);
                if (animal.FarmId != query.FarmId)
                    throw new ConflictException("Only animals from the selected farm can be moved together.");
            }
            var results = new List<AnimalMovementBatchItemResult>();
            foreach (var item in query.Animals)
            {
                var request = new AnimalMovementRequest(item.SubmissionId, query.ToPaddockId,
                    query.ChangeLot ? query.ToLotId : item.ExpectedFromLotId,
                    item.ExpectedFromPaddockId, item.ExpectedFromLotId, query.Reason);
                var result = await RecordCoreAsync(item.AnimalId, request, ct);
                results.Add(new(item.AnimalId, result.Id, result.Replayed, result.Data));
            }
            return new AnimalMovementBatchResult(results, results.All(x => x.Replayed));
        }, ct);
    private static AnimalMovementRecord Read(AnimalMovement m) => new(m.Id, m.FromPaddockId, m.ToPaddockId, m.FromLotId, m.ToLotId, m.Date, m.Reason, m.UserId);
}
