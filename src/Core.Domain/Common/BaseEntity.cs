namespace Core.Domain.Common;

public abstract class BaseEntity
{
    protected BaseEntity() : this(Guid.NewGuid()) { }
    protected BaseEntity(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("An entity identifier is required.", nameof(id));
        Id = id; CreatedAt = DateTime.UtcNow;
    }
    public Guid Id { get; }
    public DateTime CreatedAt { get; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedByUserId { get; private set; }
    public void MarkDeleted(Guid? actor)
    {
        if (IsDeleted) return;
        IsDeleted = true; DeletedAt = DateTime.UtcNow; DeletedByUserId = actor;
    }
    public void Restore()
    {
        IsDeleted = false; DeletedAt = null; DeletedByUserId = null;
    }
}
