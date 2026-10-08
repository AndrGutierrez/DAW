using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;
namespace Core.Application.Operations;

public sealed class AnalyticsService(IOperationsReader reader, IFarmAccess farms)
{
    public async Task<AnalyticsResult> GetAsync(PeriodQuery q, CancellationToken ct = default) { await new PeriodQueryValidator().ValidateAndThrowAsync(q, ct); return Calculate(q, await reader.AnalyticsAsync(q, await farms.GetAccessibleFarmIdsAsync(ct), ct)); }
    public static AnalyticsResult Calculate(PeriodQuery q, AnalyticsInputs input)
    {
        var valuations = input.Inventory.GroupBy(i => i.Category).OrderBy(g => g.Key).Select(g => new Valuation(g.Key, g.Sum(i => i.Stock * i.Cost), g.Sum(i => i.Stock * i.Price))).ToList();
        var stocks = input.Inventory.Select(i => {
            var movements = input.Movements.Where(m => m.FarmId == i.FarmId && m.ProductId == i.ProductId).ToList();
            var baseline = movements.Where(m => m.ReferenceType == "OpeningBalance" && m.ReferenceId == i.Id).OrderBy(m => m.CreatedAt).FirstOrDefault();
            var traced = baseline == null ? [] : movements.Where(m => m.Id != baseline.Id && m.CreatedAt >= baseline.CreatedAt).ToList();
            var outflow = traced.Where(m => m.Type == StockMovementType.Out && m.Date >= q.From && m.Date <= q.To).Sum(m => m.Quantity);
            decimal? rotation = null;
            if (baseline != null && baseline.Date < q.From && !traced.Any(m => m.Type == StockMovementType.Adjustment) && baseline.Quantity + traced.Sum(Delta) == i.Stock)
            {
                var opening = baseline.Quantity + traced.Where(m => m.Date < q.From).Sum(Delta);
                var closing = opening + traced.Where(m => m.Date >= q.From && m.Date <= q.To).Sum(Delta);
                var average = (opening + closing) / 2;
                if (opening >= 0 && closing >= 0 && average > 0) rotation = decimal.Round(outflow / average, 4);
            }
            return new StockMetric(i.Id, i.Farm, i.Product, i.Unit, i.Stock, i.Min, i.Max, outflow, rotation, baseline?.Date);
        }).OrderBy(i => i.Product).ToList();
        var milk = input.Milk.Where(m => m.Unit == MeasurementUnit.Liter).ToList();
        var byDay = milk.GroupBy(m => m.Date).OrderBy(g => g.Key).Select(g => new SeriesPoint(g.Key.ToString("yyyy-MM-dd"), g.Sum(m => m.Quantity), g.Count())).ToList();
        var byLot = milk.GroupBy(m => m.CurrentLot).OrderBy(g => g.Key).Select(g => new SeriesPoint(g.Key, g.Sum(m => m.Quantity), g.Count())).ToList();
        var dailyLots = milk.GroupBy(m => new { m.FarmId, m.CurrentLotId, m.CurrentLot }).OrderBy(g => g.Key.CurrentLot).ThenBy(g => g.Key.FarmId).ThenBy(g => g.Key.CurrentLotId)
            .Select(g => new DailyLotSeries(g.Key.FarmId, g.Key.CurrentLotId, g.Key.CurrentLot, g.GroupBy(m => m.Date).OrderBy(day => day.Key)
                .Select(day => new SeriesPoint(day.Key.ToString("yyyy-MM-dd"), day.Sum(m => m.Quantity), day.Count())).ToArray())).ToArray();
        var latest = input.Weights.GroupBy(w => w.AnimalId).Select(g => g.OrderByDescending(w => w.Date).ThenByDescending(w => w.CreatedAt).ThenByDescending(w => w.Id).First()).ToList();
        var valid = latest.Where(w => w.BirthDate != null && w.BirthDate <= w.Date).ToList();
        var weight = valid.GroupBy(w => w.Date < w.BirthDate!.Value.AddMonths(6) ? "0–5 meses" : w.Date < w.BirthDate.Value.AddMonths(12) ? "6–11 meses" : w.Date < w.BirthDate.Value.AddMonths(24) ? "12–23 meses" : "24 meses o más")
            .OrderBy(g => g.Key).Select(g => new SeriesPoint(g.Key, decimal.Round(g.Average(w => w.WeightKg), 2), g.Count())).ToList();
        var positive = input.Checks.Count(c => c.Result == PregnancyResult.Positive); var negative = input.Checks.Count(c => c.Result == PregnancyResult.Negative); var uncertain = input.Checks.Count(c => c.Result == PregnancyResult.Uncertain);
        return new(DateTime.UtcNow, q.From, q.To, valuations.Sum(v => v.Cost), valuations.Sum(v => v.ReferenceValue), stocks.Count(i => i.Stock <= i.Min), stocks.Count(i => i.Stock >= i.Max), valuations, stocks, byDay, byLot, input.Milk.Count - milk.Count, weight, latest.Count - valid.Count,
            positive, negative, uncertain, positive + negative == 0 ? null : decimal.Round(100m * positive / (positive + negative), 2), input.Calvings.Count, input.Calvings.Sum(c => c.OffspringCount - c.StillbornCount), input.Calvings.Sum(c => c.StillbornCount), dailyLots, ReproductionAnalytics.Calculate(q, input.ReproductiveEvents ?? input.Checks.Cast<ReproductiveEvent>().Concat(input.Calvings).ToArray()));
    }
    private static decimal Delta(StockMovement m) => m.Type == StockMovementType.In ? m.Quantity : m.Type == StockMovementType.Out ? -m.Quantity : 0;
}
public sealed class ReportService(IOperationsReader reader, IFarmAccess farms)
{
    public async Task<ReportResult> ExportAsync(PeriodQuery q, bool clinical, CancellationToken ct = default)
    {
        await new PeriodQueryValidator().ValidateAndThrowAsync(q, ct);
        return await reader.ReportAsync(new(q.From, q.To, q.FarmId, 1, 10000), clinical, await farms.GetAccessibleFarmIdsAsync(ct), ct);
    }
    public async Task<ReportResult> GetAsync(ReportQuery q, bool clinical, CancellationToken ct = default)
    {
        await new PeriodQueryValidator().ValidateAndThrowAsync(new(q.From, q.To, q.FarmId), ct);
        await new Core.Application.Livestock.CarePageRequestValidator().ValidateAndThrowAsync(new(q.Page, q.PageSize), ct);
        return await reader.ReportAsync(q, clinical, await farms.GetAccessibleFarmIdsAsync(ct), ct);
    }
}
