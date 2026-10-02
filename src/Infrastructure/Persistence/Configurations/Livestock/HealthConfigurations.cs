using Core.Domain.Livestock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Livestock;

public sealed class HealthEventConfiguration : IEntityTypeConfiguration<HealthEvent>
{
    public void Configure(EntityTypeBuilder<HealthEvent> builder)
    {
        builder.ToTable("HealthEvents");
        builder.HasKey(healthEvent => healthEvent.Id);

        builder.Property(healthEvent => healthEvent.Notes).HasMaxLength(1000);
        builder.Property(healthEvent => healthEvent.Cost).HasPrecision(18, 2);

        builder.HasDiscriminator<string>("EventType")
            .HasValue<Vaccination>("Vaccination")
            .HasValue<Treatment>("Treatment")
            .HasValue<DiseaseCase>("DiseaseCase")
            .HasValue<Deworming>("Deworming")
            .HasValue<Quarantine>("Quarantine")
            .HasValue<MortalityEvent>("Mortality");

        builder.HasIndex(healthEvent => new { healthEvent.AnimalId, healthEvent.Date });

        builder.HasOne(healthEvent => healthEvent.Farm)
            .WithMany()
            .HasForeignKey(healthEvent => healthEvent.FarmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(healthEvent => healthEvent.Animal)
            .WithMany()
            .HasForeignKey(healthEvent => healthEvent.AnimalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class VaccinationConfiguration : IEntityTypeConfiguration<Vaccination>
{
    public void Configure(EntityTypeBuilder<Vaccination> builder)
    {
        builder.Property(vaccination => vaccination.Dose).HasPrecision(10, 3);

        builder.HasOne(vaccination => vaccination.Product)
            .WithMany()
            .HasForeignKey(vaccination => vaccination.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(vaccination => vaccination.ProductBatch)
            .WithMany()
            .HasForeignKey(vaccination => vaccination.ProductBatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TreatmentConfiguration : IEntityTypeConfiguration<Treatment>
{
    public void Configure(EntityTypeBuilder<Treatment> builder)
    {
        builder.Property(treatment => treatment.Dose).HasPrecision(10, 3);
        builder.Property(treatment => treatment.Route).HasConversion<int>().IsRequired();

        builder.HasOne(treatment => treatment.Product)
            .WithMany()
            .HasForeignKey(treatment => treatment.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(treatment => treatment.ProductBatch)
            .WithMany()
            .HasForeignKey(treatment => treatment.ProductBatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DiseaseCaseConfiguration : IEntityTypeConfiguration<DiseaseCase>
{
    public void Configure(EntityTypeBuilder<DiseaseCase> builder)
    {
        builder.Property(diseaseCase => diseaseCase.Severity).HasMaxLength(50);

        builder.HasOne(diseaseCase => diseaseCase.Disease)
            .WithMany()
            .HasForeignKey(diseaseCase => diseaseCase.DiseaseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DewormingConfiguration : IEntityTypeConfiguration<Deworming>
{
    public void Configure(EntityTypeBuilder<Deworming> builder)
    {
        builder.Property(deworming => deworming.Dose).HasPrecision(10, 3);

        builder.HasOne(deworming => deworming.Product)
            .WithMany()
            .HasForeignKey(deworming => deworming.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class QuarantineConfiguration : IEntityTypeConfiguration<Quarantine>
{
    public void Configure(EntityTypeBuilder<Quarantine> builder)
    {
        builder.Property(quarantine => quarantine.Reason).HasMaxLength(500);
    }
}

public sealed class MortalityEventConfiguration : IEntityTypeConfiguration<MortalityEvent>
{
    public void Configure(EntityTypeBuilder<MortalityEvent> builder)
    {
        builder.Property(mortality => mortality.Cause).HasMaxLength(300);
        builder.Property(mortality => mortality.NecropsyNotes).HasMaxLength(1000);
    }
}

public sealed class HealthStatusChangeConfiguration : IEntityTypeConfiguration<HealthStatusChange>
{
    public void Configure(EntityTypeBuilder<HealthStatusChange> builder)
    {
        builder.ToTable("HealthStatusChanges");
        builder.HasKey(change => change.Id);

        builder.Property(change => change.PreviousStatus).HasConversion<int>().IsRequired();
        builder.Property(change => change.NewStatus).HasConversion<int>().IsRequired();
        builder.Property(change => change.Reason).HasMaxLength(500);

        builder.HasIndex(change => new { change.AnimalId, change.ChangedAt });

        builder.HasOne(change => change.Animal)
            .WithMany()
            .HasForeignKey(change => change.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
