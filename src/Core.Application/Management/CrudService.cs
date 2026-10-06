using System.Linq.Expressions;
using Core.Application.Security;
using Core.Domain.Common;
using Core.Domain.Livestock;
using FluentValidation;

namespace Core.Application.Management;

public interface IResourceDefinition<TEntity, TRequest>
    where TEntity : BaseEntity, new()
{
    Guid? FarmId(TEntity entity);
    Guid? FarmId(TRequest request);
    Expression<Func<TEntity, bool>>? Scope(IReadOnlyCollection<Guid> farmIds);
    TRequest Read(TEntity entity);
    void Apply(TEntity entity, TRequest request);
    Task CheckAsync(TEntity entity, TRequest request, CancellationToken ct);
    Task BeforeDeleteAsync(TEntity entity, CancellationToken ct);
}

public sealed class CrudService<TEntity, TRequest>(IManagementRepository repository, IResourceDefinition<TEntity, TRequest> definition, IValidator<TRequest> validator, IFarmAccess farms) : ICrudService<TRequest> where TEntity : BaseEntity, new()
{
    public async Task<IReadOnlyList<ResourceResult<TRequest>>> ListAsync(CancellationToken ct = default)
    {
        var accessible = await farms.GetAccessibleFarmIdsAsync(ct);
        var records = await repository.ListAsync(definition.Scope(accessible), ct);
        return records.Select(Result).ToList();
    }

    public async Task<ResourceResult<TRequest>> GetAsync(Guid id, CancellationToken ct = default) => Result(await FindAsync(id, false, ct));
    public Task<ResourceResult<TRequest>> CreateAsync(TRequest request, CancellationToken ct = default) => repository.ExecuteWriteAsync(async () =>
    {
        await ValidateAsync(request, ct);
        var entity = new TEntity();
        await definition.CheckAsync(entity, request, ct);
        definition.Apply(entity, request);
        repository.Add(entity);
        await repository.SaveAsync(ct);
        return Result(entity);
    }, ct);

    public Task<ResourceResult<TRequest>> UpdateAsync(Guid id, TRequest request, CancellationToken ct = default) => repository.ExecuteWriteAsync(async () =>
    {
        await ValidateAsync(request, ct);
        var entity = await FindAsync(id, true, ct);
        // History and dependent records keep their farm/animal ownership.
        if (definition.FarmId(entity) is Guid existingFarm && definition.FarmId(request) is Guid requestedFarm && existingFarm != requestedFarm)
            throw new ConflictException("Moving a record between farms requires a dedicated transfer workflow.");
        await definition.CheckAsync(entity, request, ct);
        definition.Apply(entity, request);
        await repository.SaveAsync(ct);
        return Result(entity);
    }, ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default) => repository.ExecuteWriteAsync(async () =>
    {
        var entity = await FindAsync(id, true, ct);
        await definition.BeforeDeleteAsync(entity, ct);
        repository.Remove(entity);
        await repository.SaveAsync(ct);
        return true;
    }, ct);

    private async Task ValidateAsync(TRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        if (definition.FarmId(request) is Guid farmId && !await farms.CanAccessAsync(farmId, ct))
            throw new ForbiddenException("You do not have access to this farm.");
    }

    private async Task<TEntity> FindAsync(Guid id, bool tracking, CancellationToken ct)
    {
        var entity = await repository.GetAsync<TEntity>(id, tracking, ct) ?? throw new KeyNotFoundException("The requested record was not found.");
        if (definition.FarmId(entity) is Guid farmId && !await farms.CanAccessAsync(farmId, ct))
            throw new KeyNotFoundException("The requested record was not found.");
        return entity;
    }

    private ResourceResult<TRequest> Result(TEntity entity) => new(entity.Id, entity.CreatedAt, definition.Read(entity));
}

public sealed class AnimalHealthService(IManagementRepository repository, IFarmAccess farms, IValidator<HealthUpdateRequest> validator, ICurrentUser user)
{
    public Task UpdateAsync(Guid animalId, HealthUpdateRequest request, CancellationToken ct = default) => repository.ExecuteWriteAsync(async () =>
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var animal = await repository.GetAsync<Animal>(animalId, true, ct) ?? throw new KeyNotFoundException("The requested animal was not found.");
        if (!await farms.CanAccessAsync(animal.FarmId, ct))
            throw new KeyNotFoundException("The requested animal was not found.");
        if (animal.Status != AnimalStatus.Active)
            throw new ConflictException("Only active animals can change health status.");
        if (animal.HealthStatus != request.HealthStatus)
        {
            repository.Add(new HealthStatusChange { FarmId = animal.FarmId, AnimalId = animal.Id, PreviousStatus = animal.HealthStatus, NewStatus = request.HealthStatus, Reason = request.Reason, UserId = user.UserId });
            animal.HealthStatus = request.HealthStatus;
            animal.UpdatedAt = DateTime.UtcNow;
        }

        await repository.SaveAsync(ct);
        return true;
    }, ct);
}
