using Core.Domain.Cattle;

namespace Core.Application.Cattle;

public interface ICattleRepository
{
    bool TryAddHerd(Herd herd);
    Herd? FindHerd(Guid id);
    IReadOnlyList<Herd> ListHerds();
    bool TryAddAnimal(Animal animal);
    Animal? FindAnimal(Guid id);
    Animal? UpdateAnimalHealthStatus(Guid id, AnimalHealthStatus status);
    IReadOnlyList<Animal> ListAnimals();
}
