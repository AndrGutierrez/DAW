using Core.Domain.Livestock;
namespace Core.Application.Operations;

public sealed record ReproductionMetrics(int EvaluatedFemales, int PregnantFemales, int UncertainFemales,
    decimal? PregnancyPercent, int ServedFemales, int EvaluatedServices, int PositiveServices,
    int PendingServices, decimal? FertilityPercent);

public static class ReproductionAnalytics
{
    public static ReproductionMetrics Calculate(PeriodQuery period, IReadOnlyList<ReproductiveEvent> source)
    {
        var events = source.Where(e => e.Date >= period.From && e.Date <= period.To).ToArray();
        var latestChecks = events.OfType<PregnancyCheck>().GroupBy(e => e.DamId)
            .Select(g => Latest(g)).ToArray();
        var conclusive = latestChecks.Where(c => c.Result != PregnancyResult.Uncertain).ToArray();
        var pregnant = conclusive.Count(c => c.Result == PregnancyResult.Positive &&
            !events.Any(e => e.DamId == c.DamId && (e is Calving or Abortion) && After(e, c)));
        var services = events.Where(e => e is Mating or Insemination).GroupBy(e => e.DamId)
            .Select(g => Latest(g)).ToArray();
        var outcomes = services.Select(s => events.OfType<PregnancyCheck>()
            .Where(c => c.DamId == s.DamId && After(c, s)).OrderByDescending(c => c.Date)
            .ThenByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id).FirstOrDefault()).ToArray();
        var evaluated = outcomes.Count(c => c != null && c.Result != PregnancyResult.Uncertain);
        var positive = outcomes.Count(c => c?.Result == PregnancyResult.Positive);
        return new(conclusive.Length, pregnant, latestChecks.Length - conclusive.Length,
            Percent(pregnant, conclusive.Length), services.Length, evaluated, positive,
            services.Length - evaluated, Percent(positive, evaluated));
    }
    private static T Latest<T>(IEnumerable<T> events) where T : ReproductiveEvent =>
        events.OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id).First();
    private static bool After(ReproductiveEvent value, ReproductiveEvent reference) =>
        value.Date > reference.Date || (value.Date == reference.Date &&
            (value.CreatedAt > reference.CreatedAt || (value.CreatedAt == reference.CreatedAt && value.Id.CompareTo(reference.Id) > 0)));
    private static decimal? Percent(int numerator, int denominator) =>
        denominator == 0 ? null : decimal.Round(100m * numerator / denominator, 2);
}
