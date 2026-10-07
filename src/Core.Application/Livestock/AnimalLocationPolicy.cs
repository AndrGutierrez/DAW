using Core.Application.Management;
using Core.Domain.Livestock;

namespace Core.Application.Livestock;

public sealed class AnimalLocationPolicy(IManagementRepository repository)
{
    public Task CheckAsync(Animal animal, Guid? paddockId, Guid? lotId, CancellationToken ct = default) =>
        CheckAsync(animal.Id, animal.FarmId, animal.SpeciesId, animal.Status, paddockId, lotId, ct);

    public async Task CheckAsync(Guid animalId, Guid farmId, Guid speciesId, AnimalStatus status, Guid? paddockId, Guid? lotId, CancellationToken ct = default)
    {
        if (lotId is Guid l)
        {
            var lot = await repository.GetAsync<Lot>(l, ct: ct) ?? throw new ArgumentException("The referenced Lot does not exist.");
            if (lot.FarmId != farmId || lot.SpeciesId != speciesId || !lot.IsActive)
                throw new ConflictException("The lot does not match the farm and species.");
        }
        if (paddockId is not Guid p) return;
        var paddock = await repository.GetAsync<Paddock>(p, ct: ct) ?? throw new ArgumentException("The referenced Paddock does not exist.");
        if (paddock.FarmId != farmId || !paddock.IsActive)
            throw new ConflictException("The paddock does not belong to this farm or is inactive.");
        if (status != AnimalStatus.Active || paddock.Capacity is not int capacity) return;
        var occupied = await repository.CountAsync<Animal>(a => a.Id != animalId && a.PaddockId == p && a.Status == AnimalStatus.Active, ct);
        if (occupied >= capacity)
            throw new ConflictException("The paddock has reached its configured capacity.");
    }
}
