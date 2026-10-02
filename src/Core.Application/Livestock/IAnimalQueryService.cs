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
    DateTime UpdatedAt);

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
    string? Notes);

public interface IAnimalQueryService
{
    Task<IReadOnlyList<AnimalListItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<AnimalDetail> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnimalListItem>> ListStaleAsync(int days, CancellationToken cancellationToken = default);
}
