using Core.Domain.Livestock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Livestock;

public sealed class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        builder.ToTable("Animals");
        builder.HasKey(animal => animal.Id);

        builder.Property(animal => animal.InternalTag).IsRequired().HasMaxLength(50);
        builder.Property(animal => animal.OfficialId).HasMaxLength(50);
        builder.Property(animal => animal.Rfid).HasMaxLength(50);
        builder.Property(animal => animal.Name).HasMaxLength(100);
        builder.Property(animal => animal.Color).HasMaxLength(50);
        builder.Property(animal => animal.Markings).HasMaxLength(200);
        builder.Property(animal => animal.UpdatedAt).IsRequired().HasDefaultValueSql("now()");
        builder.Property(animal => animal.Notes).HasMaxLength(2000);

        builder.Property(animal => animal.Sex).HasConversion<int>().IsRequired();
        builder.Property(animal => animal.Status).HasConversion<int>().IsRequired();
        builder.Property(animal => animal.Origin).HasConversion<int>().IsRequired();
        builder.Property(animal => animal.Purpose).HasConversion<int>().IsRequired();
        builder.Property(animal => animal.HealthStatus).HasConversion<int>().IsRequired();

        builder.HasIndex(animal => new { animal.FarmId, animal.InternalTag }).IsUnique();
        builder.HasIndex(animal => new { animal.FarmId, animal.OfficialId }).IsUnique();
        builder.HasIndex(animal => new { animal.FarmId, animal.Rfid }).IsUnique();

        builder.HasOne(animal => animal.Farm)
            .WithMany(farm => farm.Animals)
            .HasForeignKey(animal => animal.FarmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(animal => animal.Species)
            .WithMany(species => species.Animals)
            .HasForeignKey(animal => animal.SpeciesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(animal => animal.Breed)
            .WithMany(breed => breed.Animals)
            .HasForeignKey(animal => animal.BreedId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(animal => animal.Lot)
            .WithMany(lot => lot.Animals)
            .HasForeignKey(animal => animal.LotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(animal => animal.Paddock)
            .WithMany()
            .HasForeignKey(animal => animal.PaddockId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(animal => animal.Dam)
            .WithMany(animal => animal.Offspring)
            .HasForeignKey(animal => animal.DamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(animal => animal.Sire)
            .WithMany()
            .HasForeignKey(animal => animal.SireId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(animal => animal.Photos)
            .WithOne(photo => photo.Animal)
            .HasForeignKey(photo => photo.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AnimalPhotoConfiguration : IEntityTypeConfiguration<AnimalPhoto>
{
    public void Configure(EntityTypeBuilder<AnimalPhoto> builder)
    {
        builder.ToTable("AnimalPhotos");
        builder.HasKey(photo => photo.Id);

        builder.Property(photo => photo.Url).IsRequired().HasMaxLength(500);
        builder.Property(photo => photo.FileName).IsRequired().HasMaxLength(255);
        builder.Property(photo => photo.ContentType).HasMaxLength(100);
        builder.Property(photo => photo.UploadedAt).IsRequired();

        builder.HasIndex(photo => new { photo.AnimalId, photo.UploadedAt });
    }
}

public sealed class WeightRecordConfiguration : IEntityTypeConfiguration<WeightRecord>
{
    public void Configure(EntityTypeBuilder<WeightRecord> builder)
    {
        builder.ToTable("WeightRecords");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.WeightKg).HasPrecision(8, 2).IsRequired();
        builder.Property(record => record.BodyConditionScore).HasPrecision(4, 2);
        builder.Property(record => record.Notes).HasMaxLength(500);

        builder.HasIndex(record => new { record.AnimalId, record.Date });

        builder.HasOne(record => record.Animal)
            .WithMany(animal => animal.WeightRecords)
            .HasForeignKey(record => record.AnimalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
