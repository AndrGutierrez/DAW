using Core.Domain.Livestock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Livestock;

public sealed class RationConfiguration : IEntityTypeConfiguration<Ration>
{
    public void Configure(EntityTypeBuilder<Ration> builder)
    {
        builder.ToTable("Rations");
        builder.HasKey(ration => ration.Id);

        builder.Property(ration => ration.Name).IsRequired().HasMaxLength(150);
        builder.Property(ration => ration.Purpose).HasMaxLength(200);

        builder.HasOne(ration => ration.Farm)
            .WithMany()
            .HasForeignKey(ration => ration.FarmId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ration => ration.Species)
            .WithMany()
            .HasForeignKey(ration => ration.SpeciesId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RationIngredientConfiguration : IEntityTypeConfiguration<RationIngredient>
{
    public void Configure(EntityTypeBuilder<RationIngredient> builder)
    {
        builder.ToTable("RationIngredients");
        builder.HasKey(ingredient => ingredient.Id);

        builder.Property(ingredient => ingredient.Quantity).HasPrecision(10, 3).IsRequired();
        builder.Property(ingredient => ingredient.Unit).HasConversion<int>().IsRequired();

        builder.HasOne(ingredient => ingredient.Ration)
            .WithMany(ration => ration.Ingredients)
            .HasForeignKey(ingredient => ingredient.RationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ingredient => ingredient.Product)
            .WithMany()
            .HasForeignKey(ingredient => ingredient.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FeedingRecordConfiguration : IEntityTypeConfiguration<FeedingRecord>
{
    public void Configure(EntityTypeBuilder<FeedingRecord> builder)
    {
        builder.ToTable("FeedingRecords");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.QuantityKg).HasPrecision(10, 3).IsRequired();
        builder.Property(record => record.Cost).HasPrecision(12, 2);

        builder.HasIndex(record => new { record.LotId, record.Date });

        builder.HasOne(record => record.Lot)
            .WithMany()
            .HasForeignKey(record => record.LotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(record => record.Ration)
            .WithMany()
            .HasForeignKey(record => record.RationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductBatchConfiguration : IEntityTypeConfiguration<ProductBatch>
{
    public void Configure(EntityTypeBuilder<ProductBatch> builder)
    {
        builder.ToTable("ProductBatches");
        builder.HasKey(batch => batch.Id);

        builder.Property(batch => batch.BatchNumber).HasMaxLength(50);
        builder.Property(batch => batch.InitialQuantity).HasPrecision(12, 3).IsRequired();
        builder.Property(batch => batch.UnitCost).HasPrecision(12, 4);

        builder.HasIndex(batch => new { batch.FarmId, batch.ProductId, batch.BatchNumber });

        builder.HasOne(batch => batch.Farm)
            .WithMany()
            .HasForeignKey(batch => batch.FarmId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(batch => batch.Product)
            .WithMany()
            .HasForeignKey(batch => batch.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(batch => batch.Supplier)
            .WithMany()
            .HasForeignKey(batch => batch.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.Type).HasConversion<int>().IsRequired();
        builder.Property(movement => movement.Quantity).HasPrecision(12, 3).IsRequired();
        builder.Property(movement => movement.Reason).HasMaxLength(300);
        builder.Property(movement => movement.ReferenceType).HasMaxLength(100);

        builder.HasIndex(movement => new { movement.FarmId, movement.ProductId, movement.Date });

        builder.HasOne(movement => movement.Product)
            .WithMany()
            .HasForeignKey(movement => movement.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(movement => movement.ProductBatch)
            .WithMany()
            .HasForeignKey(movement => movement.ProductBatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
