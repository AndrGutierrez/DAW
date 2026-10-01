using Core.Domain.Common;

namespace Core.Domain.Livestock;

public sealed class Animal : BaseEntity
{
    public Guid FarmId { get; set; }

    public Farm Farm { get; set; } = null!;

    public Guid SpeciesId { get; set; }

    public Species Species { get; set; } = null!;

    public Guid? BreedId { get; set; }

    public Breed? Breed { get; set; }

    public Guid? LotId { get; set; }

    public Lot? Lot { get; set; }

    public Guid? PaddockId { get; set; }

    public Paddock? Paddock { get; set; }

    public string InternalTag { get; set; } = null!;

    public string? OfficialId { get; set; }

    public string? Rfid { get; set; }

    public string? Name { get; set; }

    public Sex Sex { get; set; }

    public DateOnly? BirthDate { get; set; }

    public decimal? BirthWeightKg { get; set; }

    public string? Color { get; set; }

    public string? Markings { get; set; }

    public AnimalStatus Status { get; set; } = AnimalStatus.Active;

    public AnimalOrigin Origin { get; set; } = AnimalOrigin.Born;

    public ProductivePurpose Purpose { get; set; }

    public HealthStatus HealthStatus { get; set; } = HealthStatus.Healthy;

    public Guid? DamId { get; set; }

    public Animal? Dam { get; set; }

    public Guid? SireId { get; set; }

    public Animal? Sire { get; set; }

    public ICollection<Animal> Offspring { get; set; } = [];

    public string? PhotoUrl { get; set; }

    public string? Notes { get; set; }

    public ICollection<WeightRecord> WeightRecords { get; set; } = [];
}

public sealed class WeightRecord : BaseEntity
{
    public Guid FarmId { get; set; }

    public Guid AnimalId { get; set; }

    public Animal Animal { get; set; } = null!;

    public DateOnly Date { get; set; }

    public decimal WeightKg { get; set; }

    public decimal? BodyConditionScore { get; set; }

    public Guid? RecordedByUserId { get; set; }

    public string? Notes { get; set; }
}
