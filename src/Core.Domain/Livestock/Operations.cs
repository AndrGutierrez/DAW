using Core.Domain.Common;

namespace Core.Domain.Livestock;

public sealed class AnimalMovement : BaseEntity
{
    public AnimalMovement() { }
    public AnimalMovement(Guid id) : base(id) { }

    public Guid FarmId { get; set; }

    public Guid AnimalId { get; set; }

    public Animal Animal { get; set; } = null!;

    public Guid? FromPaddockId { get; set; }

    public Paddock? FromPaddock { get; set; }

    public Guid? ToPaddockId { get; set; }

    public Paddock? ToPaddock { get; set; }

    public Guid? FromLotId { get; set; }

    public Lot? FromLot { get; set; }

    public Guid? ToLotId { get; set; }

    public Lot? ToLot { get; set; }

    public DateOnly Date { get; set; }

    public string? Reason { get; set; }

    public Guid? UserId { get; set; }
}

public sealed class TaskItem : BaseEntity
{
    public Guid FarmId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public TaskType Type { get; set; }

    public DateOnly? DueDate { get; set; }

    public TaskPriority Priority { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    public Guid? AssignedToUserId { get; set; }

    public Guid? RelatedAnimalId { get; set; }

    public Animal? RelatedAnimal { get; set; }

    public Guid? RelatedLotId { get; set; }

    public Lot? RelatedLot { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime? CompletedAt { get; set; }
}

public sealed class AlertRule : BaseEntity
{
    public Guid FarmId { get; set; }

    public AlertType Type { get; set; }

    public decimal? ThresholdValue { get; set; }

    public int? ThresholdDays { get; set; }

    public bool IsEnabled { get; set; } = true;
}

public sealed class Alert : BaseEntity
{
    public Guid FarmId { get; set; }

    public AlertType Type { get; set; }

    public AlertSeverity Severity { get; set; }

    public string Message { get; set; } = null!;

    public string? RelatedEntityType { get; set; }

    public Guid? RelatedEntityId { get; set; }

    public DateOnly? DueDate { get; set; }

    public bool IsResolved { get; set; }

    public DateTime? ResolvedAt { get; set; }
}

public sealed class Transaction : BaseEntity
{
    public Guid FarmId { get; set; }

    public TransactionType Type { get; set; }

    public TransactionCategory Category { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public DateOnly Date { get; set; }

    public string? Description { get; set; }

    public string? Counterparty { get; set; }

    public Guid? AnimalId { get; set; }

    public Animal? Animal { get; set; }

    public Guid? LotId { get; set; }

    public Lot? Lot { get; set; }

    public Guid? ProductId { get; set; }

    public Product? Product { get; set; }

    public Guid? UserId { get; set; }
}

public sealed class Attachment : BaseEntity
{
    public Guid FarmId { get; set; }

    public string OwnerType { get; set; } = null!;

    public Guid OwnerId { get; set; }

    public string FileName { get; set; } = null!;

    public string Url { get; set; } = null!;

    public string? ContentType { get; set; }

    public long? SizeBytes { get; set; }

    public Guid? UploadedByUserId { get; set; }
}

public sealed class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }

    public Guid? FarmId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public string? EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
