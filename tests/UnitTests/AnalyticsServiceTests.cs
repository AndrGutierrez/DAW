using Core.Application.Operations;
using Core.Domain.Livestock;
namespace UnitTests;
public sealed class AnalyticsServiceTests
{
    private readonly DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
    private AnalyticsInputs Inputs => new([new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Farm", "Feed", "Nutrition", MeasurementUnit.Kilogram, 5m, 5m, 100m, 2m, 3m)], [], [], [], [], []);
    private PeriodQuery Period => new(today, today);
    [Fact]
    public void ValuationUsesStockTimesEachCatalogPriceAndIncludesThresholdBoundary()
    {
        var result = AnalyticsService.Calculate(Period, Inputs); Assert.Equal(10m, result.Cost); Assert.Equal(15m, result.ReferenceValue); Assert.Equal(1, result.Critical); Assert.Equal(0, result.Excess); Assert.Null(result.Stock[0].Rotation); Assert.Null(result.PositiveCheckPercent);
    }
    [Fact]
    public void MaximumBoundaryIsExcessAndNotCritical()
    {
        var source = Inputs; var result = AnalyticsService.Calculate(Period, source with { Inventory = [source.Inventory[0] with { Stock = 100 }] }); Assert.Equal(1, result.Excess); Assert.Equal(0, result.Critical);
    }
    [Fact]
    public void RotationRequiresCompletePeriodAndConsistentClosingBalance()
    {
        var source = Inputs; var inventory = source.Inventory[0];
        var baseline = new StockMovement { FarmId = inventory.FarmId, ProductId = inventory.ProductId, Quantity = 10, ReferenceType = "OpeningBalance", ReferenceId = inventory.Id, Type = StockMovementType.Adjustment, Date = today.AddDays(-1) };
        var movement = new StockMovement { FarmId = inventory.FarmId, ProductId = inventory.ProductId, Quantity = 5, Type = StockMovementType.Out, Date = today };
        source = source with { Movements = [baseline, movement] };
        Assert.Equal(decimal.Round(5m / 7.5m, 4), AnalyticsService.Calculate(Period, source).Stock[0].Rotation);
        Assert.Null(AnalyticsService.Calculate(Period with { From = today.AddDays(-1) }, source).Stock[0].Rotation);
        Assert.Null(AnalyticsService.Calculate(Period, source with { Inventory = [inventory with { Stock = 4 }] }).Stock[0].Rotation);
    }
    [Fact]
    public void MilkDoesNotSumIncompatibleUnits()
    {
        var result = AnalyticsService.Calculate(Period, Inputs with { Milk = [new(today, "Lot A", 10, MeasurementUnit.Liter), new(today, "Lot A", 5, MeasurementUnit.Kilogram)] });
        Assert.Equal(10, Assert.Single(result.MilkByDay).Value); Assert.Equal(1, result.ExcludedMilk);
    }
    [Fact]
    public void DiagnosticProportionUsesOnlyConclusiveChecks()
    {
        var result = AnalyticsService.Calculate(Period, Inputs with { Checks = [new() { Result = PregnancyResult.Positive }, new() { Result = PregnancyResult.Negative }, new() { Result = PregnancyResult.Uncertain }] });
        Assert.Equal(50m, result.PositiveCheckPercent); Assert.Equal(1, result.UncertainChecks);
    }
    [Fact]
    public void WeightUsesLastRecordPerAnimalAndAgeAtMeasurement()
    {
        var animal = Guid.NewGuid();
        var result = AnalyticsService.Calculate(Period, Inputs with { Weights = [new(animal, Guid.NewGuid(), DateTime.UtcNow, today.AddDays(-1), today.AddMonths(-7), 110), new(animal, Guid.NewGuid(), DateTime.UtcNow, today, today.AddMonths(-7), 120), new(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, today, null, 500)] });
        var point = Assert.Single(result.WeightByAge); Assert.Equal("6–11 meses", point.Label); Assert.Equal(120, point.Value); Assert.Equal(1, point.Count); Assert.Equal(1, result.ExcludedWeights);
    }
    [Fact]
    public void ZeroAverageStockHasNoInventedRotation()
    {
        var source = Inputs; var i = source.Inventory[0] with { Stock = 0 };
        var baseline = new StockMovement { FarmId = i.FarmId, ProductId = i.ProductId, ReferenceType = "OpeningBalance", ReferenceId = i.Id, Date = today };
        var result = AnalyticsService.Calculate(Period, source with { Inventory = [i], Movements = [baseline] }); Assert.Null(result.Stock[0].Rotation);
    }
}
