namespace Core.Application.Livestock;

public sealed record AnimalPhotoInfo(Guid Id, string Url, DateTime UploadedAt);

public sealed record AnimalListItem(
    Guid Id,
    string InternalTag,
    string? Name,
    string Species,
    string? Breed,
    string Sex,
    string Status,
    string HealthStatus,
    DateOnly? BirthDate,
    decimal? CurrentWeightKg,
    string? Lot,
    string Farm,
    string? CoverPhotoUrl,
    int PhotoCount,
    DateTime UpdatedAt,
    Guid FarmId,
    Guid SpeciesId,
    Guid? LotId = null,
    Guid? PaddockId = null);

public sealed record AnimalDetail(
    Guid Id,
    string InternalTag,
    string? OfficialId,
    string? Rfid,
    string? Name,
    string Species,
    string? Breed,
    string Sex,
    string Status,
    string HealthStatus,
    string Origin,
    string Purpose,
    DateOnly? BirthDate,
    decimal? BirthWeightKg,
    decimal? CurrentWeightKg,
    decimal? BodyConditionScore,
    string? Color,
    string? Markings,
    string? Lot,
    string? Paddock,
    string Farm,
    IReadOnlyList<AnimalPhotoInfo> Photos,
    DateTime UpdatedAt,
    string? Notes,
    Guid FarmId,
    Guid SpeciesId,
    Guid? BreedId,
    Guid? LotId,
    Guid? PaddockId,
    Guid? DamId,
    Guid? SireId);

public sealed record AnimalLineage(Guid Id, string InternalTag, string? Name, Guid? DamId, Guid? SireId, bool IsArchived);

public interface IAnimalQueryService
{
    Task<AnimalPageResult> PageAsync(AnimalPageRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnimalListItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<AnimalLineage> GetLineageAsync(Guid id, CancellationToken ct = default);

    Task<AnimalDetail> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnimalListItem>> ListStaleAsync(int days, CancellationToken cancellationToken = default);
}
