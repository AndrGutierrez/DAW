using Core.Application.Cattle;
using Core.Domain.Cattle;

namespace Infrastructure.Cattle;

public sealed class InMemoryCattleRepository(InMemoryCattleStore store) : ICattleRepository
{
    public bool TryAddHerd(Herd herd)
    {
        lock (store.Gate)
        {
            if (store.Herds.Values.Any(existing =>
                string.Equals(existing.Name, herd.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            store.Herds.Add(herd.Id, herd);
            return true;
        }
    }

    public Herd? FindHerd(Guid id)
    {
        lock (store.Gate)
        {
            return store.Herds.GetValueOrDefault(id);
        }
    }

    public IReadOnlyList<Herd> ListHerds()
    {
        lock (store.Gate)
        {
            return store.Herds.Values.OrderBy(herd => herd.Name).ToArray();
        }
    }

    public bool TryAddAnimal(Animal animal)
    {
        lock (store.Gate)
        {
            if (store.Animals.Values.Any(existing =>
                string.Equals(existing.EarTag, animal.EarTag, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            store.Animals.Add(animal.Id, animal);
            return true;
        }
    }

    public Animal? FindAnimal(Guid id)
    {
        lock (store.Gate)
        {
            return store.Animals.GetValueOrDefault(id);
        }
    }

    public Animal? UpdateAnimalHealthStatus(Guid id, AnimalHealthStatus status)
    {
        lock (store.Gate)
        {
            var animal = store.Animals.GetValueOrDefault(id);
            animal?.ChangeHealthStatus(status);
            return animal;
        }
    }

    public IReadOnlyList<Animal> ListAnimals()
    {
        lock (store.Gate)
        {
            return store.Animals.Values.OrderBy(animal => animal.EarTag).ToArray();
        }
    }
}
