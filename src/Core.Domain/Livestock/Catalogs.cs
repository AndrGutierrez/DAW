using Core.Domain.Common;

namespace Core.Domain.Livestock;

public sealed class Species : BaseEntity
{
    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public ProductivePurpose Purpose { get; set; }

    public int? GestationDays { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Breed> Breeds { get; set; } = [];

    public ICollection<Animal> Animals { get; set; } = [];

    public ICollection<Lot> Lots { get; set; } = [];
}

public sealed class Breed : BaseEntity
{
    public Guid SpeciesId { get; set; }

    public Species Species { get; set; } = null!;

    public string Name { get; set; } = null!;

    public ProductivePurpose Purpose { get; set; }

    public string? Origin { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Animal> Animals { get; set; } = [];
}

public sealed class Disease : BaseEntity
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsNotifiable { get; set; }

    public Guid? SpeciesId { get; set; }

    public Species? Species { get; set; }
}

public sealed class Product : BaseEntity
{
    public string SKU { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public InventoryCategory InventoryCategory { get; set; } = null!;
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public string Brand { get; set; } = "Generic";
    public string Name { get; set; } = null!;

    public MeasurementUnit Unit { get; set; } = MeasurementUnit.Unit;

    public int? WithdrawalDays { get; set; }

    public bool RequiresPrescription { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class Supplier : BaseEntity
{
    public string Name { get; set; } = null!;

    public string? ContactName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }
}

public sealed class InventoryCategory : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Product> Products { get; set; } = [];
}

public sealed class FarmInventory : BaseEntity
{
    public Guid FarmId { get; set; }
    public Farm Farm { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal Stock { get; set; }
    public decimal MinStock { get; set; } = 5;
    public decimal MaxStock { get; set; } = 100;
    public string Location { get; set; } = "Main warehouse";
}
