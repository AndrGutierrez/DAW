using Core.Application.Operations;
using Core.Domain.Livestock;
namespace UnitTests;
public sealed class ReproductionAnalyticsTests
{
    private readonly PeriodQuery period = new(new(2026, 1, 1), new(2026, 1, 31));
    private static T Event<T>(Guid dam, int day, int hour = 8) where T : ReproductiveEvent, new() =>
        EntityTestTime.At(new T { DamId = dam, Date = new(2026, 1, day) }, new(2026, 1, day, hour, 0, 0, DateTimeKind.Utc));
    private static PregnancyCheck Check(Guid dam, int day, PregnancyResult result, int hour = 10)
    { var c = Event<PregnancyCheck>(dam, day, hour); c.Result = result; return c; }
    [Fact]
    public void NoObservationsProduceNoInventedRates()
    { var r = ReproductionAnalytics.Calculate(period, []); Assert.Null(r.PregnancyPercent); Assert.Null(r.FertilityPercent); Assert.Equal(0, r.ServedFemales); }
    [Fact]
    public void RepeatedDiagnosticsCountOneFemaleAndUseHerLatestResult()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var r = ReproductionAnalytics.Calculate(period, [Check(a, 2, PregnancyResult.Positive), Check(a, 3, PregnancyResult.Positive), Check(a, 4, PregnancyResult.Negative), Check(b, 5, PregnancyResult.Positive)]);
        Assert.Equal(2, r.EvaluatedFemales); Assert.Equal(1, r.PregnantFemales); Assert.Equal(50m, r.PregnancyPercent);
    }
    [Fact]
    public void LatestUncertaintyIsExcludedInsteadOfReusingOlderPositiveResult()
    {
        var a = Guid.NewGuid(); var r = ReproductionAnalytics.Calculate(period, [Check(a, 2, PregnancyResult.Positive), Check(a, 3, PregnancyResult.Uncertain)]);
        Assert.Null(r.PregnancyPercent); Assert.Equal(1, r.UncertainFemales); Assert.Equal(0, r.PregnantFemales);
    }
    [Theory] [InlineData(true)] [InlineData(false)]
    public void ASubsequentCalvingOrAbortionEndsPregnancyWithoutErasingTheEvaluation(bool calving)
    {
        var a = Guid.NewGuid(); ReproductiveEvent end = calving ? Event<Calving>(a, 15) : Event<Abortion>(a, 15);
        var r = ReproductionAnalytics.Calculate(period, [Check(a, 5, PregnancyResult.Positive), end]);
        Assert.Equal(1, r.EvaluatedFemales); Assert.Equal(0m, r.PregnancyPercent);
    }
    [Fact]
    public void FertilityUsesDistinctServedFemalesAndSeparatesPendingDiagnoses()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid(); var c = Guid.NewGuid();
        var r = ReproductionAnalytics.Calculate(period, [Event<Mating>(a, 2), Event<Mating>(a, 3), Event<Insemination>(b, 3), Event<Mating>(c, 3), Check(a, 8, PregnancyResult.Positive), Check(b, 8, PregnancyResult.Negative), Check(c, 8, PregnancyResult.Uncertain)]);
        Assert.Equal(3, r.ServedFemales); Assert.Equal(2, r.EvaluatedServices); Assert.Equal(1, r.PendingServices); Assert.Equal(50m, r.FertilityPercent);
    }
    [Fact]
    public void DiagnosisBeforeLatestServiceCannotBeAttributedToIt()
    {
        var a = Guid.NewGuid(); var r = ReproductionAnalytics.Calculate(period, [Event<Mating>(a, 2), Check(a, 5, PregnancyResult.Positive), Event<Insemination>(a, 8)]);
        Assert.Equal(1, r.PendingServices); Assert.Null(r.FertilityPercent);
    }
    [Theory] [InlineData(7, false)] [InlineData(9, true)]
    public void SameDayChronologyUsesRecordedTimeInsteadOfAssumingServiceCameFirst(int diagnosisHour, bool evaluated)
    {
        var a = Guid.NewGuid(); var r = ReproductionAnalytics.Calculate(period, [Event<Mating>(a, 2, 8), Check(a, 2, PregnancyResult.Positive, diagnosisHour)]);
        Assert.Equal(evaluated ? 1 : 0, r.EvaluatedServices); Assert.Equal(evaluated ? 100m : null, r.FertilityPercent);
    }
    [Fact]
    public void EventsOutsideRequestedPeriodDoNotChangeCohortOrOutcome()
    {
        var a = Guid.NewGuid(); var r = ReproductionAnalytics.Calculate(period with { From = new(2026, 1, 5), To = new(2026, 1, 10) }, [Event<Mating>(a, 2), Check(a, 7, PregnancyResult.Negative), Check(a, 15, PregnancyResult.Positive)]);
        Assert.Equal(0, r.ServedFemales); Assert.Equal(0m, r.PregnancyPercent);
    }
}
