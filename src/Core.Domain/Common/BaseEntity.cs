namespace Core.Domain.Common;

public abstract class BaseEntity
{
    protected BaseEntity() : this(Guid.NewGuid()) { }

    protected BaseEntity(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("An entity identifier is required.", nameof(id));
        Id = id;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; }

    public DateTime CreatedAt { get; }
}
