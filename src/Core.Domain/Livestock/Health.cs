using Core.Domain.Common;

namespace Core.Domain.Livestock;

public abstract class HealthEvent : BaseEntity
{
    public Guid FarmId { get; set; }

    public Farm Farm { get; set; } = null!;

    public Guid AnimalId { get; set; }

    public Animal Animal { get; set; } = null!;

    public DateOnly Date { get; set; }

    public Guid? UserId { get; set; }

    public string? Notes { get; set; }

    public decimal? Cost { get; set; }
}

public sealed class Vaccination : HealthEvent
{
    public Guid? ProductId { get; set; }

    public Product? Product { get; set; }

    public Guid? ProductBatchId { get; set; }

    public ProductBatch? ProductBatch { get; set; }

    public decimal? Dose { get; set; }

    public DateOnly? NextDueDate { get; set; }
}

public sealed class Treatment : HealthEvent
{
    public Guid? ProductId { get; set; }

    public Product? Product { get; set; }

    public Guid? ProductBatchId { get; set; }

    public ProductBatch? ProductBatch { get; set; }

    public decimal? Dose { get; set; }

    public MedicationRoute Route { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int? WithdrawalDays { get; set; }

    public DateOnly? WithdrawalEndDate { get; set; }
}

public sealed class DiseaseCase : HealthEvent
{
    public Guid? DiseaseId { get; set; }

    public Disease? Disease { get; set; }

    public string? Severity { get; set; }

    public bool IsContagious { get; set; }
}

public sealed class Deworming : HealthEvent
{
    public Guid? ProductId { get; set; }

    public Product? Product { get; set; }

    public decimal? Dose { get; set; }
}

public sealed class Quarantine : HealthEvent
{
    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Reason { get; set; }
}

public sealed class MortalityEvent : HealthEvent
{
    public string? Cause { get; set; }

    public string? NecropsyNotes { get; set; }
}

public sealed class HealthStatusChange : BaseEntity
{
    public Guid FarmId { get; set; }

    public Guid AnimalId { get; set; }

    public Animal Animal { get; set; } = null!;

    public HealthStatus PreviousStatus { get; set; }

    public HealthStatus NewStatus { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public string? Reason { get; set; }

    public Guid? UserId { get; set; }
}
