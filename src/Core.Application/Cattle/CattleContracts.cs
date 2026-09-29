using Core.Domain.Cattle;

namespace Core.Application.Cattle;

public sealed record HerdResult(Guid Id, string Name, string? Description, DateTime CreatedAt);

public sealed record AnimalResult(
    Guid Id,
    string EarTag,
    string Breed,
    DateOnly? DateOfBirth,
    AnimalHealthStatus HealthStatus,
    Guid HerdId,
    string HerdName,
    DateTime CreatedAt);
