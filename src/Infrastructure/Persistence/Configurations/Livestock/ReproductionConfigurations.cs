using Core.Domain.Livestock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Livestock;

public sealed class ReproductiveEventConfiguration : IEntityTypeConfiguration<ReproductiveEvent>
{
    public void Configure(EntityTypeBuilder<ReproductiveEvent> builder)
    {
        builder.ToTable("ReproductiveEvents");
        builder.HasKey(reproductiveEvent => reproductiveEvent.Id);

        builder.Property(reproductiveEvent => reproductiveEvent.Notes).HasMaxLength(1000);

        builder.HasDiscriminator<string>("EventType")
            .HasValue<Heat>("Heat")
            .HasValue<Mating>("Mating")
            .HasValue<Insemination>("Insemination")
            .HasValue<PregnancyCheck>("PregnancyCheck")
            .HasValue<Calving>("Calving")
            .HasValue<Weaning>("Weaning")
            .HasValue<Abortion>("Abortion");

        builder.HasIndex(reproductiveEvent => new { reproductiveEvent.DamId, reproductiveEvent.Date });

        builder.HasOne(reproductiveEvent => reproductiveEvent.Farm)
            .WithMany()
            .HasForeignKey(reproductiveEvent => reproductiveEvent.FarmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(reproductiveEvent => reproductiveEvent.Dam)
            .WithMany()
            .HasForeignKey(reproductiveEvent => reproductiveEvent.DamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HeatConfiguration : IEntityTypeConfiguration<Heat>
{
    public void Configure(EntityTypeBuilder<Heat> builder)
    {
        builder.Property(heat => heat.Method).HasMaxLength(50);
    }
}

public sealed class MatingConfiguration : IEntityTypeConfiguration<Mating>
{
    public void Configure(EntityTypeBuilder<Mating> builder)
    {
        builder.Property(mating => mating.Method).HasConversion<int>().IsRequired();

        builder.HasOne(mating => mating.Sire)
            .WithMany()
            .HasForeignKey(mating => mating.SireId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InseminationConfiguration : IEntityTypeConfiguration<Insemination>
{
    public void Configure(EntityTypeBuilder<Insemination> builder)
    {
        builder.HasOne(insemination => insemination.SemenBatch)
            .WithMany()
            .HasForeignKey(insemination => insemination.SemenBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(insemination => insemination.Sire)
            .WithMany()
            .HasForeignKey(insemination => insemination.SireId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PregnancyCheckConfiguration : IEntityTypeConfiguration<PregnancyCheck>
{
    public void Configure(EntityTypeBuilder<PregnancyCheck> builder)
    {
        builder.Property(check => check.Result).HasConversion<int>().IsRequired();
        builder.Property(check => check.Method).HasMaxLength(50);
    }
}

public sealed class CalvingConfiguration : IEntityTypeConfiguration<Calving>
{
    public void Configure(EntityTypeBuilder<Calving> builder)
    {
        builder.Property(calving => calving.Difficulty).HasConversion<int>().IsRequired();
    }
}

public sealed class WeaningConfiguration : IEntityTypeConfiguration<Weaning>
{
    public void Configure(EntityTypeBuilder<Weaning> builder)
    {
        builder.Property(weaning => weaning.WeightKg).HasPrecision(8, 2);

        builder.HasOne(weaning => weaning.Offspring)
            .WithMany()
            .HasForeignKey(weaning => weaning.OffspringId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AbortionConfiguration : IEntityTypeConfiguration<Abortion>
{
    public void Configure(EntityTypeBuilder<Abortion> builder)
    {
        builder.Property(abortion => abortion.Reason).HasMaxLength(500);
    }
}

public sealed class SemenBatchConfiguration : IEntityTypeConfiguration<SemenBatch>
{
    public void Configure(EntityTypeBuilder<SemenBatch> builder)
    {
        builder.ToTable("SemenBatches");
        builder.HasKey(batch => batch.Id);

        builder.Property(batch => batch.SireName).HasMaxLength(150);
        builder.Property(batch => batch.BatchNumber).HasMaxLength(50);
        builder.Property(batch => batch.StorageTank).HasMaxLength(50);

        builder.HasOne(batch => batch.Farm)
            .WithMany()
            .HasForeignKey(batch => batch.FarmId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(batch => batch.Breed)
            .WithMany()
            .HasForeignKey(batch => batch.BreedId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(batch => batch.Supplier)
            .WithMany()
            .HasForeignKey(batch => batch.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
