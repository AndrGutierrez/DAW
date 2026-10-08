using Core.Domain.Common;
namespace Core.Domain.Livestock;
public sealed class ExchangeRate : BaseEntity
{
    public ExchangeRate() { }
    public ExchangeRate(Guid id) : base(id) { }
    public DateOnly EffectiveDate { get; set; }
    public decimal BolivarsPerDollar { get; set; }
    public Guid? RecordedByUserId { get; set; }
}
