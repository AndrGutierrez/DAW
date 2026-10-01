using Core.Domain.Common;

namespace Core.Domain.Livestock;

public sealed class MilkProductionRecord : BaseEntity
{
    public Guid FarmId { get; set; }

    public Guid AnimalId { get; set; }

    public Animal Animal { get; set; } = null!;

    public DateOnly Date { get; set; }

    public MilkShift Shift { get; set; }

    public decimal Liters { get; set; }

    public decimal? FatPercent { get; set; }

    public decimal? ProteinPercent { get; set; }

    public int? SomaticCellCount { get; set; }
}

public sealed class EggProductionRecord : BaseEntity
{
    public Guid FarmId { get; set; }

    public Guid LotId { get; set; }

    public Lot Lot { get; set; } = null!;

    public DateOnly Date { get; set; }

    public int TotalEggs { get; set; }

    public int BrokenEggs { get; set; }

    public decimal? AverageWeightGrams { get; set; }
}

public sealed class WoolProductionRecord : BaseEntity
{
    public Guid FarmId { get; set; }

    public Guid AnimalId { get; set; }

    public Animal Animal { get; set; } = null!;

    public DateOnly Date { get; set; }

    public decimal FleeceWeightKg { get; set; }

    public decimal? FiberDiameterMicrons { get; set; }

    public string? Grade { get; set; }
}

public sealed class SlaughterRecord : BaseEntity
{
    public Guid FarmId { get; set; }

    public Guid AnimalId { get; set; }

    public Animal Animal { get; set; } = null!;

    public DateOnly Date { get; set; }

    public decimal? LiveWeightKg { get; set; }

    public decimal? CarcassWeightKg { get; set; }

    public decimal? ColdCarcassWeightKg { get; set; }

    public string? Grade { get; set; }
}
