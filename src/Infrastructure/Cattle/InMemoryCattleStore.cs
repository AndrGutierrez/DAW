using Core.Domain.Cattle;

namespace Infrastructure.Cattle;

// Temporary shared state for the Phase 1 demo. Persistent storage belongs to Phase 2.
public sealed class InMemoryCattleStore
{
    internal object Gate { get; } = new();
    internal Dictionary<Guid, Herd> Herds { get; } = [];
    internal Dictionary<Guid, Animal> Animals { get; } = [];
}
