using Core.Domain.Cattle;

namespace Core.Application.Cattle;

public sealed class CattleCatalogService(
    ICattleRepository repository,
    IAnimalTagNormalizer tagNormalizer,
    IAnimalRegistrationValidator registrationValidator) : ICattleCatalogService
{
    public HerdResult CreateHerd(string name, string? description)
    {
        var herd = new Herd(name, description);

        if (!repository.TryAddHerd(herd))
        {
            throw new InvalidOperationException("A herd with this name already exists.");
        }

        return ToResult(herd);
    }

    public IReadOnlyList<HerdResult> ListHerds() =>
        repository.ListHerds().Select(ToResult).ToArray();

    public HerdResult GetHerd(Guid id)
    {
        var herd = repository.FindHerd(id)
            ?? throw new KeyNotFoundException("The requested herd was not found.");

        return ToResult(herd);
    }

    public AnimalResult RegisterAnimal(string earTag, string breed, Guid herdId, DateOnly? dateOfBirth)
    {
        registrationValidator.Validate(breed, dateOfBirth);

        var herd = repository.FindHerd(herdId)
            ?? throw new KeyNotFoundException("The requested herd was not found.");

        var animal = new Animal(tagNormalizer.Normalize(earTag), breed, herd, dateOfBirth);

        if (!repository.TryAddAnimal(animal))
        {
            throw new InvalidOperationException("An animal with this ear tag already exists.");
        }

        return ToResult(animal);
    }

    public AnimalResult GetAnimal(Guid id)
    {
        var animal = repository.FindAnimal(id)
            ?? throw new KeyNotFoundException("The requested animal was not found.");

        return ToResult(animal);
    }

    public AnimalResult UpdateAnimalHealthStatus(Guid id, AnimalHealthStatus status)
    {
        var animal = repository.UpdateAnimalHealthStatus(id, status)
            ?? throw new KeyNotFoundException("The requested animal was not found.");

        return ToResult(animal);
    }

    public IReadOnlyList<AnimalResult> ListAnimals() =>
        repository.ListAnimals().Select(ToResult).ToArray();

    private static HerdResult ToResult(Herd herd) =>
        new(herd.Id, herd.Name, herd.Description, herd.CreatedAt);

    private static AnimalResult ToResult(Animal animal) =>
        new(
            animal.Id,
            animal.EarTag,
            animal.Breed,
            animal.DateOfBirth,
            animal.HealthStatus,
            animal.HerdId,
            animal.Herd.Name,
            animal.CreatedAt);
}
