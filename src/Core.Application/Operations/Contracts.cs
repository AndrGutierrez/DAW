using Core.Application.Livestock;
using Core.Application.Management;
using Core.Domain.Livestock;
using FluentValidation;
namespace Core.Application.Operations;

public sealed record InventoryQuery(int Page = 1, int PageSize = 12, Guid? FarmId = null, string? Search = null, string? State = null);
public sealed record InventoryRow(Guid Id, InventoryRequest Data, string Farm, string Product, string SKU, string Category, MeasurementUnit Unit, bool ProductActive);
public sealed record ProductQuery(int Page = 1, int PageSize = 12, string? Search = null, Guid? CategoryId = null, bool? IsActive = null);
public sealed record StockRequest(Guid SubmissionId, StockMovementType Type, decimal Quantity, decimal ExpectedStock, string Reason, Guid? AnimalId = null);
public sealed record StockRecord(Guid Id, DateOnly Date, StockMovementType Type, decimal Quantity, string? Reason, Guid? AnimalId, Guid? UserId, bool OpeningBalance);
public sealed record PeriodQuery(DateOnly From, DateOnly To, Guid? FarmId = null);
public sealed record ReportQuery(DateOnly From, DateOnly To, Guid? FarmId = null, int Page = 1, int PageSize = 100);
public sealed record ReportRow(Guid Id, DateOnly Date, string Farm, string Animal, string Kind, string? Product, decimal? Quantity, string? Unit, string? Notes, DateOnly? WithdrawalEndDate, string? Detail);
public sealed record ReportResult(CarePage<ReportRow> Records, DateTime GeneratedAt);
public sealed record Valuation(string Category, decimal Cost, decimal ReferenceValue);
public sealed record StockMetric(Guid Id, string Farm, string Product, MeasurementUnit Unit, decimal Stock, decimal Min, decimal Max, decimal Outflow, decimal? Rotation, DateOnly? HistorySince);
public sealed record SeriesPoint(string Label, decimal Value, int Count);
public sealed record DailyLotSeries(Guid FarmId, Guid? LotId, string Label, IReadOnlyList<SeriesPoint> Points);
public sealed record AnalyticsResult(DateTime GeneratedAt, DateOnly From, DateOnly To, decimal Cost, decimal ReferenceValue,
    int Critical, int Excess, IReadOnlyList<Valuation> Categories, IReadOnlyList<StockMetric> Stock,
    IReadOnlyList<SeriesPoint> MilkByDay, IReadOnlyList<SeriesPoint> MilkByCurrentLot, int ExcludedMilk,
    IReadOnlyList<SeriesPoint> WeightByAge, int ExcludedWeights, int PositiveChecks, int NegativeChecks, int UncertainChecks,
    decimal? PositiveCheckPercent, int Calvings, int LiveBirths, int Stillbirths, IReadOnlyList<DailyLotSeries> MilkByDayAndCurrentLot, ReproductionMetrics Reproduction);
public interface IOperationsReader
{
    Task<CarePage<InventoryRow>> InventoryAsync(InventoryQuery query, IReadOnlyCollection<Guid> farms, CancellationToken ct);
    Task<CarePage<ResourceResult<ProductRequest>>> ProductsAsync(ProductQuery query, CancellationToken ct);
    Task<AnalyticsInputs> AnalyticsAsync(PeriodQuery query, IReadOnlyCollection<Guid> farms, CancellationToken ct);
    Task<ReportResult> ReportAsync(ReportQuery query, bool clinical, IReadOnlyCollection<Guid> farms, CancellationToken ct);
}
public sealed record InventoryInput(Guid Id, Guid FarmId, Guid ProductId, string Farm, string Product, string Category, MeasurementUnit Unit, decimal Stock, decimal Min, decimal Max, decimal Cost, decimal Price);
public sealed record MilkInput(DateOnly Date, string CurrentLot, decimal Quantity, MeasurementUnit Unit, Guid FarmId = default, Guid? CurrentLotId = null);
public sealed record WeightInput(Guid AnimalId, Guid Id, DateTime CreatedAt, DateOnly Date, DateOnly? BirthDate, decimal WeightKg);
public sealed record AnalyticsInputs(IReadOnlyList<InventoryInput> Inventory, IReadOnlyList<StockMovement> Movements, IReadOnlyList<MilkInput> Milk,
    IReadOnlyList<WeightInput> Weights, IReadOnlyList<PregnancyCheck> Checks, IReadOnlyList<Calving> Calvings, IReadOnlyList<ReproductiveEvent>? ReproductiveEvents = null);
public sealed class InventoryQueryValidator : AbstractValidator<InventoryQuery>
{
    public InventoryQueryValidator() { RuleFor(q => q.Page).InclusiveBetween(1, 100000); RuleFor(q => q.PageSize).InclusiveBetween(1, 100); RuleFor(q => q.Search).MaximumLength(100); RuleFor(q => q.State).Must(s => s == null || s == "Low" || s == "High" || s == "Normal"); }
}
public sealed class ProductQueryValidator : AbstractValidator<ProductQuery>
{
    public ProductQueryValidator() { RuleFor(q => q.Page).InclusiveBetween(1, 100000); RuleFor(q => q.PageSize).InclusiveBetween(1, 100); RuleFor(q => q.Search).MaximumLength(100); }
}
public sealed class PeriodQueryValidator : AbstractValidator<PeriodQuery>
{
    public PeriodQueryValidator() { RuleFor(q => q.From).NotEmpty(); RuleFor(q => q.To).NotEmpty().GreaterThanOrEqualTo(q => q.From).LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow)); RuleFor(q => q).Must(q => q.To.DayNumber - q.From.DayNumber <= 366).WithMessage("Choose a period of at most 367 days."); RuleFor(q => q.FarmId).NotEqual(Guid.Empty).When(q => q.FarmId.HasValue); }
}
public sealed class StockRequestValidator : AbstractValidator<StockRequest>
{
    public StockRequestValidator() { RuleFor(q => q.SubmissionId).NotEmpty(); RuleFor(q => q.Type).Must(t => t is StockMovementType.In or StockMovementType.Out); RuleFor(q => q.Quantity).GreaterThan(0).PrecisionScale(14, 4, true); RuleFor(q => q.ExpectedStock).GreaterThanOrEqualTo(0).PrecisionScale(14, 4, true); RuleFor(q => q.Reason).NotEmpty().MaximumLength(300); RuleFor(q => q.AnimalId).NotEqual(Guid.Empty).When(q => q.AnimalId.HasValue); RuleFor(q => q.Type).Equal(StockMovementType.Out).When(q => q.AnimalId.HasValue); }
}
