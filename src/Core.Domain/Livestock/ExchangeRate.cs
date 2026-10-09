using Core.Domain.Common;
namespace Core.Domain.Livestock;
public sealed class ExchangeRate : BaseEntity
{
    public ExchangeRate() { }
    public ExchangeRate(Guid id) : base(id) { }
    public DateOnly EffectiveDate { get; set; }
    public decimal BolivarsPerDollar { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public string Source { get; set; } = "https://www.bcv.org.ve/";
    public string EntryMethod { get; set; } = "manual";
    public DateTime? PublishedAt { get; set; }
}
