using Core.Domain.Common;

namespace Core.Domain.Livestock;

public sealed class Ration : BaseEntity
{
    public Guid FarmId { get; set; }

    public Farm Farm { get; set; } = null!;

    public Guid? SpeciesId { get; set; }

    public Species? Species { get; set; }

    public string Name { get; set; } = null!;

    public string? Purpose { get; set; }

    public ICollection<RationIngredient> Ingredients { get; set; } = [];
}

public sealed class RationIngredient : BaseEntity
{
    public Guid RationId { get; set; }

    public Ration Ration { get; set; } = null!;

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }

    public MeasurementUnit Unit { get; set; }
}

public sealed class FeedingRecord : BaseEntity
{
    public Guid FarmId { get; set; }

    public Guid LotId { get; set; }

    public Lot Lot { get; set; } = null!;

    public Guid RationId { get; set; }

    public Ration Ration { get; set; } = null!;

    public DateOnly Date { get; set; }

    public decimal QuantityKg { get; set; }

    public decimal? Cost { get; set; }
}

public sealed class ProductBatch : BaseEntity
{
    public Guid FarmId { get; set; }

    public Farm Farm { get; set; } = null!;

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public Guid? SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public string? BatchNumber { get; set; }

    public DateOnly? ExpirationDate { get; set; }

    public decimal InitialQuantity { get; set; }

    public decimal? UnitCost { get; set; }
}

public sealed class StockMovement : BaseEntity
{
    public StockMovement() { }
    public StockMovement(Guid id) : base(id) { }
    public Guid FarmId { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public Guid? ProductBatchId { get; set; }

    public ProductBatch? ProductBatch { get; set; }

    public StockMovementType Type { get; set; }

    public decimal Quantity { get; set; }

    public string? Reason { get; set; }

    public DateOnly Date { get; set; }

    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public Guid? UserId { get; set; }
}
