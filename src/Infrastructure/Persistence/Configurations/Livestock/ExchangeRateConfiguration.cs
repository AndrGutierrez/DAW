using Core.Domain.Livestock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Infrastructure.Persistence.Configurations.Livestock;
public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("ExchangeRates"); builder.HasKey(r => r.Id);
        builder.Property(r => r.Source).HasMaxLength(400).HasDefaultValue("https://www.bcv.org.ve/");
        builder.Property(r => r.EntryMethod).HasMaxLength(20).HasDefaultValue("manual");
        builder.Property(r => r.BolivarsPerDollar).HasPrecision(18, 6).IsRequired();
        builder.HasIndex(r => new { r.EffectiveDate, r.CreatedAt });
    }
}
