using System.Linq.Expressions;
using Core.Application.Livestock;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using Moq;

namespace UnitTests;

public sealed class GrowthMonitoringServiceTests
{
    private readonly Mock<IManagementRepository> repository = new(MockBehavior.Strict);
    private readonly Mock<IFarmAccess> farms = new(MockBehavior.Strict);
    private readonly Mock<IGrowthMonitoringReader> reader = new(MockBehavior.Strict);
    private readonly Farm farm = new() { Name = "Test farm", Code = "TEST" };
    private readonly List<AlertRule> policies = [];
    private readonly List<GrowthObservation> observations = [];
    private GrowthMonitoringService Service => new(repository.Object, farms.Object, reader.Object);
    public GrowthMonitoringServiceTests()
    {
        farms.Setup(f => f.GetAccessibleFarmIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => new[] { farm.Id });
        farms.Setup(f => f.CanAccessAsync(farm.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(r => r.GetAsync<Farm>(farm.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(farm);
        repository.Setup(r => r.ListAsync(It.IsAny<Expression<Func<AlertRule, bool>>>(), It.IsAny<CancellationToken>())).Returns((Expression<Func<AlertRule, bool>> p, CancellationToken _) => Task.FromResult<IReadOnlyList<AlertRule>>(policies.Where(p.Compile()).ToList()));
        reader.Setup(r => r.ReadAsync(It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { farm.Id })), It.IsAny<CancellationToken>())).ReturnsAsync(() => observations);
    }
    private void Add(decimal current, decimal previous, decimal? goal = null, int days = 10) => observations.Add(new(Guid.NewGuid(), farm.Id, "TEST-" + observations.Count, null, goal, new DateOnly(2026, 1, 1).AddDays(days), current, new DateOnly(2026, 1, 1), previous));
    [Fact]
    public async Task LossGeneratesAnAlertWithoutInventingATarget()
    {
        Add(99, 100);
        var result = await Service.AlertsAsync(farm.Id, new());
        var alert = Assert.Single(result.Items); Assert.Equal("weight-loss", alert.Reason); Assert.Equal(-0.1m, alert.DailyGainKg); Assert.Null(alert.TargetDailyGainKg);
    }
    [Fact]
    public async Task IndividualGoalOverridesFarmPolicyAndEqualityDoesNotAlert()
    {
        policies.Add(new() { FarmId = farm.Id, Type = AlertType.LowWeightGain, ThresholdValue = 1m });
        Add(105, 100, .5m); Add(105, 100);
        var alert = Assert.Single((await Service.AlertsAsync(farm.Id, new())).Items);
        Assert.Equal("farm", alert.TargetSource); Assert.Equal("below-target", alert.Reason);
    }
    [Fact]
    public async Task ComparisonUsesUnroundedGain()
    {
        Add(100.01m, 100, .0001m, 101);
        var alert = Assert.Single((await Service.AlertsAsync(null, new())).Items);
        Assert.Equal(.0001m, alert.DailyGainKg); Assert.Equal("below-target", alert.Reason);
    }
    [Fact]
    public async Task DisabledPolicyDoesNotApplyButIndividualGoalRemainsEffective()
    {
        policies.Add(new() { FarmId = farm.Id, Type = AlertType.LowWeightGain, ThresholdValue = 2, IsEnabled = false });
        Add(101, 100); Add(101, 100, 1);
        Assert.Equal("animal", Assert.Single((await Service.AlertsAsync(null, new())).Items).TargetSource);
    }
    [Fact]
    public async Task MissingAndSameDayMeasurementsAreExcludedExplicitly()
    {
        Add(90, 100, days: 0); observations.Add(new(Guid.NewGuid(), farm.Id, "NEW", null, 1, null, null, null, null));
        var result = await Service.AlertsAsync(null, new()); Assert.Empty(result.Items); Assert.Equal(2, result.InsufficientMeasurements); Assert.Equal(2, result.ActiveBovines);
    }
    [Fact]
    public async Task AlertsArePagedAfterEvaluationWithStableSeverityOrder()
    {
        for (var i = 0; i < 12; i++) Add(99 - i, 100);
        var result = await Service.AlertsAsync(null, new(2, 5)); Assert.Equal(12, result.Total); Assert.Equal(5, result.Items.Count); Assert.Equal(-.7m, result.Items[0].DailyGainKg);
    }
    [Fact]
    public async Task UnassignedFarmIsHiddenBeforeReadingMeasurements()
    {
        farms.Setup(f => f.CanAccessAsync(farm.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.AlertsAsync(farm.Id, new()));
        reader.Verify(r => r.ReadAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    public async Task InvalidGoalNeverWrites(decimal value)
    {
        repository.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<GrowthGoal>>>(), It.IsAny<CancellationToken>())).Returns((Func<Task<GrowthGoal>> action, CancellationToken _) => action());
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => Service.SetPolicyAsync(farm.Id, new(value)));
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task SavingAPolicyPersistsOneFarmRule()
    {
        repository.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<GrowthGoal>>>(), It.IsAny<CancellationToken>())).Returns((Func<Task<GrowthGoal>> action, CancellationToken _) => action());
        AlertRule? stored = null;
        repository.Setup(r => r.Add(It.IsAny<AlertRule>())).Callback<AlertRule>(rule => stored = rule);
        repository.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var goal = await Service.SetPolicyAsync(farm.Id, new(.85m));
        Assert.Equal(.85m, goal.DailyGainKg); Assert.Equal(farm.Id, stored!.FarmId); Assert.True(stored.IsEnabled);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
