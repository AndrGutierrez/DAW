using Core.Domain.Livestock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Livestock;

public sealed class SpeciesConfiguration : IEntityTypeConfiguration<Species>
{
    public void Configure(EntityTypeBuilder<Species> builder)
    {
        builder.ToTable("Species");
        builder.HasKey(species => species.Id);

        builder.Property(species => species.Name).IsRequired().HasMaxLength(100);
        builder.Property(species => species.Code).IsRequired().HasMaxLength(20);
        builder.Property(species => species.Purpose).HasConversion<int>().IsRequired();
        builder.Property(species => species.IsActive).IsRequired();

        builder.HasIndex(species => species.Code).IsUnique();
        builder.HasIndex(species => species.Name).IsUnique();
    }
}

public sealed class BreedConfiguration : IEntityTypeConfiguration<Breed>
{
    public void Configure(EntityTypeBuilder<Breed> builder)
    {
        builder.ToTable("Breeds");
        builder.HasKey(breed => breed.Id);

        builder.Property(breed => breed.Name).IsRequired().HasMaxLength(100);
        builder.Property(breed => breed.Origin).HasMaxLength(100);
        builder.Property(breed => breed.Purpose).HasConversion<int>().IsRequired();
        builder.Property(breed => breed.IsActive).IsRequired();

        builder.HasIndex(breed => new { breed.SpeciesId, breed.Name }).IsUnique();

        builder.HasOne(breed => breed.Species)
            .WithMany(species => species.Breeds)
            .HasForeignKey(breed => breed.SpeciesId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DiseaseConfiguration : IEntityTypeConfiguration<Disease>
{
    public void Configure(EntityTypeBuilder<Disease> builder)
    {
        builder.ToTable("Diseases");
        builder.HasKey(disease => disease.Id);

        builder.Property(disease => disease.Name).IsRequired().HasMaxLength(150);
        builder.Property(disease => disease.Description).HasMaxLength(1000);
        builder.Property(disease => disease.IsNotifiable).IsRequired();

        builder.HasOne(disease => disease.Species)
            .WithMany()
            .HasForeignKey(disease => disease.SpeciesId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name).IsRequired().HasMaxLength(150);
        builder.Property(product => product.Category).HasConversion<int>().IsRequired();
        builder.Property(product => product.Unit).HasConversion<int>().IsRequired();
        builder.Property(product => product.RequiresPrescription).IsRequired();
        builder.Property(product => product.IsActive).IsRequired();

        builder.HasIndex(product => product.Name);
    }
}

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(supplier => supplier.Id);

        builder.Property(supplier => supplier.Name).IsRequired().HasMaxLength(150);
        builder.Property(supplier => supplier.ContactName).HasMaxLength(150);
        builder.Property(supplier => supplier.Phone).HasMaxLength(50);
        builder.Property(supplier => supplier.Email).HasMaxLength(200);
        builder.Property(supplier => supplier.Address).HasMaxLength(300);
    }
}
