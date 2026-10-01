using Core.Domain.Livestock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Livestock;

public sealed class MilkProductionRecordConfiguration : IEntityTypeConfiguration<MilkProductionRecord>
{
    public void Configure(EntityTypeBuilder<MilkProductionRecord> builder)
    {
        builder.ToTable("MilkProductionRecords");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.Shift).HasConversion<int>().IsRequired();
        builder.Property(record => record.Liters).HasPrecision(8, 2).IsRequired();
        builder.Property(record => record.FatPercent).HasPrecision(5, 2);
        builder.Property(record => record.ProteinPercent).HasPrecision(5, 2);

        builder.HasIndex(record => new { record.AnimalId, record.Date, record.Shift });

        builder.HasOne(record => record.Animal)
            .WithMany()
            .HasForeignKey(record => record.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class EggProductionRecordConfiguration : IEntityTypeConfiguration<EggProductionRecord>
{
    public void Configure(EntityTypeBuilder<EggProductionRecord> builder)
    {
        builder.ToTable("EggProductionRecords");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.AverageWeightGrams).HasPrecision(8, 2);

        builder.HasIndex(record => new { record.LotId, record.Date });

        builder.HasOne(record => record.Lot)
            .WithMany()
            .HasForeignKey(record => record.LotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WoolProductionRecordConfiguration : IEntityTypeConfiguration<WoolProductionRecord>
{
    public void Configure(EntityTypeBuilder<WoolProductionRecord> builder)
    {
        builder.ToTable("WoolProductionRecords");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.FleeceWeightKg).HasPrecision(8, 2).IsRequired();
        builder.Property(record => record.FiberDiameterMicrons).HasPrecision(8, 2);
        builder.Property(record => record.Grade).HasMaxLength(50);

        builder.HasIndex(record => new { record.AnimalId, record.Date });

        builder.HasOne(record => record.Animal)
            .WithMany()
            .HasForeignKey(record => record.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SlaughterRecordConfiguration : IEntityTypeConfiguration<SlaughterRecord>
{
    public void Configure(EntityTypeBuilder<SlaughterRecord> builder)
    {
        builder.ToTable("SlaughterRecords");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.LiveWeightKg).HasPrecision(8, 2);
        builder.Property(record => record.CarcassWeightKg).HasPrecision(8, 2);
        builder.Property(record => record.ColdCarcassWeightKg).HasPrecision(8, 2);
        builder.Property(record => record.Grade).HasMaxLength(50);

        builder.HasIndex(record => new { record.AnimalId, record.Date });

        builder.HasOne(record => record.Animal)
            .WithMany()
            .HasForeignKey(record => record.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
