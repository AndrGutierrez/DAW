using Core.Domain.Common;

namespace Core.Domain.Livestock;

public sealed class Farm : BaseEntity
{
    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Paddock> Paddocks { get; set; } = [];

    public ICollection<Lot> Lots { get; set; } = [];

    public ICollection<Animal> Animals { get; set; } = [];

    public ICollection<UserFarm> Members { get; set; } = [];
}

public sealed class UserFarm : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid FarmId { get; set; }

    public Farm Farm { get; set; } = null!;

    public bool IsDefault { get; set; }
}

public sealed class Paddock : BaseEntity
{
    public Guid FarmId { get; set; }

    public Farm Farm { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public decimal? AreaHectares { get; set; }

    public int? Capacity { get; set; }

    public int? MaxStayDays { get; set; }

    public decimal? MapX { get; set; }
    public decimal? MapY { get; set; }
    public decimal? MapWidth { get; set; }
    public decimal? MapHeight { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class Lot : BaseEntity
{
    public Guid FarmId { get; set; }

    public Farm Farm { get; set; } = null!;

    public Guid SpeciesId { get; set; }

    public Species Species { get; set; } = null!;

    public Guid? PaddockId { get; set; }

    public Paddock? Paddock { get; set; }

    public string Name { get; set; } = null!;

    public ProductivePurpose Purpose { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Animal> Animals { get; set; } = [];
}
