using System.Linq.Expressions;
using Core.Domain.Common;
using Core.Domain.Livestock;

namespace Core.Application.Management;

public sealed record FarmRequest(string Name, string Code, string? Address = null, string? Phone = null, string? Email = null, bool IsActive = true);
public sealed record SpeciesRequest(string Name, string Code, ProductivePurpose Purpose, int? GestationDays = null, bool IsActive = true);
public sealed record BreedRequest(Guid SpeciesId, string Name, ProductivePurpose Purpose, string? Origin = null, bool IsActive = true);
public sealed record PaddockRequest(Guid FarmId, string Name, string? Code = null, decimal? AreaHectares = null, int? Capacity = null, bool IsActive = true);
public sealed record LotRequest(Guid FarmId, Guid SpeciesId, string Name, ProductivePurpose Purpose, Guid? PaddockId = null, bool IsActive = true);
public sealed record CategoryRequest(string Name, string? Description = null, bool IsActive = true);
public sealed record ProductRequest(string SKU, string Name, Guid CategoryId, decimal Price, decimal CostPrice, MeasurementUnit Unit, string Brand = "Generic", int? WithdrawalDays = null, bool RequiresPrescription = false, bool IsActive = true);
public sealed record InventoryRequest(Guid FarmId, Guid ProductId, decimal Stock, decimal MinStock = 5, decimal MaxStock = 100, string Location = "Main warehouse");
public sealed record AnimalRequest(Guid FarmId, Guid SpeciesId, string InternalTag, Sex Sex, ProductivePurpose Purpose, Guid? BreedId = null, Guid? LotId = null, Guid? PaddockId = null, string? OfficialId = null, string? Rfid = null, string? Name = null, DateOnly? BirthDate = null, decimal? BirthWeightKg = null, string? Color = null, string? Markings = null, AnimalStatus Status = AnimalStatus.Active, AnimalOrigin Origin = AnimalOrigin.Born, HealthStatus HealthStatus = HealthStatus.Healthy, Guid? DamId = null, Guid? SireId = null, string? Notes = null);
public sealed record ProductionRequest(Guid FarmId, Guid AnimalId, DateOnly Date, AnimalProductType ProductType, ProductionMethod Method, decimal Quantity, MeasurementUnit Unit, Guid OperationId, string? Notes = null);
public sealed record WeightRequest(Guid FarmId, Guid AnimalId, DateOnly Date, decimal WeightKg, decimal? BodyConditionScore = null, string? Notes = null);
public sealed record HealthUpdateRequest(HealthStatus HealthStatus, string? Reason = null);
public sealed record LegacyHealthUpdateRequest(HealthStatus Status, string? Reason = null);
public sealed record ResourceResult<T>(Guid Id, DateTime CreatedAt, T Data);
public interface ICrudService<TRequest>
{
    Task<IReadOnlyList<ResourceResult<TRequest>>> ListAsync(CancellationToken ct = default);
    Task<ResourceResult<TRequest>> GetAsync(Guid id, CancellationToken ct = default);
    Task<ResourceResult<TRequest>> CreateAsync(TRequest request, CancellationToken ct = default);
    Task<ResourceResult<TRequest>> UpdateAsync(Guid id, TRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

// Persistence port: Application depends on this contract, never on EF Core.
public interface IManagementRepository
{
    Task<TResult> ExecuteWriteAsync<TResult>(Func<Task<TResult>> operation, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        where T : BaseEntity;
    Task<int> CountAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default) where T : BaseEntity;
    Task<IReadOnlyList<T>> PageAsync<T, TOrder>(Expression<Func<T, bool>> predicate, Expression<Func<T, TOrder>> order, int skip, int take, CancellationToken ct = default) where T : BaseEntity;
    Task<T?> GetAsync<T>(Guid id, bool tracking = false, CancellationToken ct = default)
        where T : BaseEntity;
    Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : BaseEntity;
    void Add<T>(T entity)
        where T : BaseEntity;
    void Remove<T>(T entity)
        where T : BaseEntity;
    Task SaveAsync(CancellationToken ct = default);
}

public sealed class ConflictException(string message) : Exception(message);
public sealed class ForbiddenException(string message) : Exception(message);
