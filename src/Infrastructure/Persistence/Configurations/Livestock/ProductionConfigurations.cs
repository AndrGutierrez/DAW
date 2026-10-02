using Core.Domain.Livestock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Livestock;

public sealed class AnimalProductionConfiguration : IEntityTypeConfiguration<AnimalProduction>
{
    public void Configure(EntityTypeBuilder<AnimalProduction> b)
    {
        b.ToTable("AnimalProduction", t => t.HasCheckConstraint("CK_AnimalProduction_Quantity", "\"Quantity\" > 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Quantity).HasPrecision(14, 4).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.ProductType).HasConversion<int>();
        b.Property(x => x.Method).HasConversion<int>();
        b.Property(x => x.Unit).HasConversion<int>();
        b.HasIndex(x => new { x.OperationId, x.ProductType }).IsUnique();
        b.HasIndex(x => new { x.AnimalId, x.Date });
        b.HasOne(x => x.Animal).WithMany(x => x.Production).HasForeignKey(x => x.AnimalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Farm).WithMany().HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InventoryCategoryConfiguration : IEntityTypeConfiguration<InventoryCategory>
{
    public void Configure(EntityTypeBuilder<InventoryCategory> b)
    {
        b.ToTable("InventoryCategories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class FarmInventoryConfiguration : IEntityTypeConfiguration<FarmInventory>
{
    public void Configure(EntityTypeBuilder<FarmInventory> b)
    {
        b.ToTable("FarmInventory", t => t.HasCheckConstraint("CK_FarmInventory_Stock", "\"Stock\" >= 0 AND \"MinStock\" >= 0 AND \"MaxStock\" > \"MinStock\""));
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.FarmId, x.ProductId }).IsUnique();
        b.Property(x => x.Stock).HasPrecision(14, 4).HasDefaultValue(0m);
        b.Property(x => x.MinStock).HasPrecision(14, 4).HasDefaultValue(5m).HasSentinel(-1m);
        b.Property(x => x.MaxStock).HasPrecision(14, 4).HasDefaultValue(100m);
        b.Property(x => x.Location).IsRequired().HasMaxLength(150).HasDefaultValue("Main warehouse");
        b.HasOne(x => x.Farm).WithMany().HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
