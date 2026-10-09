using Core.Domain.Common;

namespace Core.Domain.Livestock;

public enum AnimalProductType { Milk, Wool, Meat, Eggs, Hide, Other }
public enum ProductionMethod { Milking, Shearing, Slaughter, Collection }

// One row describes one product obtained from one animal in one operation.
public sealed class AnimalProduction : BaseEntity
{
    public AnimalProduction() { }
    public AnimalProduction(Guid id) : base(id) { }

    public Guid FarmId { get; set; }
    public Farm Farm { get; set; } = null!;
    public Guid AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public Guid OperationId { get; set; }
    public DateOnly Date { get; set; }
    public AnimalProductType ProductType { get; set; }
    public ProductionMethod Method { get; set; }
    public decimal Quantity { get; set; }
    public MeasurementUnit Unit { get; set; }
    public string? Notes { get; set; }
}
