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
    public string Name { get; set; } = null!;

    public ProductCategory Category { get; set; }

    public MeasurementUnit Unit { get; set; }

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
