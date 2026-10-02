using Core.Domain.Livestock;
using Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Livestock;

public sealed class FarmConfiguration : IEntityTypeConfiguration<Farm>
{
    public void Configure(EntityTypeBuilder<Farm> builder)
    {
        builder.ToTable("Farms");
        builder.HasKey(farm => farm.Id);

        builder.Property(farm => farm.Name).IsRequired().HasMaxLength(150);
        builder.Property(farm => farm.Code).IsRequired().HasMaxLength(20);
        builder.Property(farm => farm.Address).HasMaxLength(300);
        builder.Property(farm => farm.Phone).HasMaxLength(50);
        builder.Property(farm => farm.Email).HasMaxLength(200);
        builder.Property(farm => farm.IsActive).IsRequired();

        builder.HasIndex(farm => farm.Code).IsUnique();
    }
}

public sealed class UserFarmConfiguration : IEntityTypeConfiguration<UserFarm>
{
    public void Configure(EntityTypeBuilder<UserFarm> builder)
    {
        builder.ToTable("UserFarms");
        builder.HasKey(userFarm => userFarm.Id);

        builder.Property(userFarm => userFarm.IsDefault).IsRequired();
        builder.HasIndex(userFarm => new { userFarm.UserId, userFarm.FarmId }).IsUnique();

        builder.HasOne(userFarm => userFarm.Farm)
            .WithMany(farm => farm.Members)
            .HasForeignKey(userFarm => userFarm.FarmId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(userFarm => userFarm.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PaddockConfiguration : IEntityTypeConfiguration<Paddock>
{
    public void Configure(EntityTypeBuilder<Paddock> builder)
    {
        builder.ToTable("Paddocks");
        builder.HasKey(paddock => paddock.Id);

        builder.Property(paddock => paddock.Name).IsRequired().HasMaxLength(150);
        builder.Property(paddock => paddock.Code).HasMaxLength(30);
        builder.Property(paddock => paddock.IsActive).IsRequired();

        builder.HasIndex(paddock => new { paddock.FarmId, paddock.Name }).IsUnique();

        builder.HasOne(paddock => paddock.Farm)
            .WithMany(farm => farm.Paddocks)
            .HasForeignKey(paddock => paddock.FarmId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LotConfiguration : IEntityTypeConfiguration<Lot>
{
    public void Configure(EntityTypeBuilder<Lot> builder)
    {
        builder.ToTable("Lots");
        builder.HasKey(lot => lot.Id);

        builder.Property(lot => lot.Name).IsRequired().HasMaxLength(150);
        builder.Property(lot => lot.Purpose).HasConversion<int>().IsRequired();
        builder.Property(lot => lot.IsActive).IsRequired();

        builder.HasIndex(lot => new { lot.FarmId, lot.Name }).IsUnique();

        builder.HasOne(lot => lot.Farm)
            .WithMany(farm => farm.Lots)
            .HasForeignKey(lot => lot.FarmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(lot => lot.Species)
            .WithMany(species => species.Lots)
            .HasForeignKey(lot => lot.SpeciesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(lot => lot.Paddock)
            .WithMany()
            .HasForeignKey(lot => lot.PaddockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
