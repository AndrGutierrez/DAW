using Core.Domain.Common;

namespace Core.Domain.Livestock;

public abstract class ReproductiveEvent : BaseEntity
{
    public Guid FarmId { get; set; }

    public Farm Farm { get; set; } = null!;

    public Guid DamId { get; set; }

    public Animal Dam { get; set; } = null!;

    public DateOnly Date { get; set; }

    public Guid? UserId { get; set; }

    public string? Notes { get; set; }
}

public sealed class Heat : ReproductiveEvent
{
    public string? Method { get; set; }
}

public sealed class Mating : ReproductiveEvent
{
    public ReproductionMethod Method { get; set; } = ReproductionMethod.Natural;

    public Guid? SireId { get; set; }

    public Animal? Sire { get; set; }

    public Guid? TechnicianUserId { get; set; }
}

public sealed class Insemination : ReproductiveEvent
{
    public Guid? SemenBatchId { get; set; }

    public SemenBatch? SemenBatch { get; set; }

    public Guid? SireId { get; set; }

    public Animal? Sire { get; set; }

    public Guid? TechnicianUserId { get; set; }
}

public sealed class PregnancyCheck : ReproductiveEvent
{
    public PregnancyResult Result { get; set; }

    public string? Method { get; set; }

    public DateOnly? ExpectedCalvingDate { get; set; }
}

public sealed class Calving : ReproductiveEvent
{
    public int OffspringCount { get; set; }

    public int StillbornCount { get; set; }

    public CalvingDifficulty Difficulty { get; set; }
}

public sealed class Weaning : ReproductiveEvent
{
    public Guid? OffspringId { get; set; }

    public Animal? Offspring { get; set; }

    public decimal? WeightKg { get; set; }
}

public sealed class Abortion : ReproductiveEvent
{
    public string? Reason { get; set; }
}

public sealed class SemenBatch : BaseEntity
{
    public Guid FarmId { get; set; }

    public Farm Farm { get; set; } = null!;

    public Guid? BreedId { get; set; }

    public Breed? Breed { get; set; }

    public string? SireName { get; set; }

    public Guid? SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public string? BatchNumber { get; set; }

    public int? StrawCount { get; set; }

    public string? StorageTank { get; set; }

    public DateOnly? ExpirationDate { get; set; }
}
