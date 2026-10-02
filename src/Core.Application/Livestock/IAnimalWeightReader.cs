namespace Core.Application.Livestock;

public sealed record LatestWeight(decimal WeightKg, decimal? BodyConditionScore, DateOnly Date);

public interface IAnimalWeightReader
{
    Task<LatestWeight?> GetLatestAsync(Guid animalId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, LatestWeight>> GetLatestForAsync(
        IReadOnlyCollection<Guid> animalIds,
        CancellationToken cancellationToken = default);
}
