using Core.Domain.Cattle;

namespace Infrastructure.Cattle;

// Shared in-memory cattle records; process restart clears the data.
public sealed class InMemoryCattleStore
{
    internal object Gate { get; } = new();
    internal Dictionary<Guid, Herd> Herds { get; } = [];
    internal Dictionary<Guid, Animal> Animals { get; } = [];
}
