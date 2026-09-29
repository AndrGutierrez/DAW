using Core.Domain.Cattle;

namespace Core.Application.Cattle;

public interface ICattleCatalogService
{
    HerdResult CreateHerd(string name, string? description);
    HerdResult GetHerd(Guid id);
    IReadOnlyList<HerdResult> ListHerds();
    AnimalResult RegisterAnimal(string earTag, string breed, Guid herdId, DateOnly? dateOfBirth);
    AnimalResult GetAnimal(Guid id);
    AnimalResult UpdateAnimalHealthStatus(Guid id, AnimalHealthStatus status);
    IReadOnlyList<AnimalResult> ListAnimals();
}
