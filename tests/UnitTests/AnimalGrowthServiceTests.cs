using System.Linq.Expressions;
using System.Reflection;
using Core.Domain.Common;
using Core.Application.Livestock;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using Moq;

namespace UnitTests;

public sealed class AnimalGrowthServiceTests
{
    private readonly Mock<IManagementRepository> repository = new(MockBehavior.Strict);
    private readonly Mock<IFarmAccess> farms = new(MockBehavior.Strict);
    private readonly Animal animal = new() { FarmId = Guid.NewGuid() };
    private readonly List<WeightRecord> records = [];
    private AnimalGrowthService Service => new(repository.Object, farms.Object);

    public AnimalGrowthServiceTests()
    {
        repository.Setup(r => r.GetAsync<Animal>(animal.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(animal);
        farms.Setup(f => f.CanAccessAsync(animal.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(r => r.ListAsync(It.IsAny<Expression<Func<WeightRecord, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Expression<Func<WeightRecord, bool>> predicate, CancellationToken _) =>
                Task.FromResult<IReadOnlyList<WeightRecord>>(records.Where(predicate.Compile()).ToArray()));
    }

    private WeightRecord Add(int day, decimal weight, int minute = 0, Guid? id = null)
    {
        var record = new WeightRecord(id ?? Guid.NewGuid()) { AnimalId = animal.Id, FarmId = animal.FarmId,
            Date = new DateOnly(2026, 1, 1).AddDays(day - 1), WeightKg = weight };
        typeof(BaseEntity).GetField("<CreatedAt>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(record, new DateTime(2026, 2, 1, 0, minute, 0, DateTimeKind.Utc));
        records.Add(record);
        return record;
    }

    [Fact]
    public async Task HistoryIsPagedOnServerWhileCurveAndTotalsStayComplete()
    {
        for (var day = 1; day <= 25; day++) Add(day, 100 + day);
        var result = await Service.GetAsync(animal.Id, new GrowthPageRequest(2, 10));
        Assert.Equal(25, result.Total);
        Assert.Equal(25, result.TotalDates);
        Assert.Equal(25, result.Points.Count);
        Assert.Equal(10, result.Records.Count);
        Assert.Equal(new DateOnly(2026, 1, 15), result.Records.First().Date);
        Assert.Equal(2, result.Page);
    }

    [Fact]
    public async Task BoundedCurveKeepsThePrecedingIntervalForItsFirstVisiblePoint()
    {
        for (var day = 1; day <= 65; day++) Add(day, 100 + day);
        var result = await Service.GetAsync(animal.Id);
        Assert.Equal(65, result.TotalDates);
        Assert.Equal(60, result.Points.Count);
        Assert.Equal(1m, result.Points.First().DailyGainKg);
        Assert.Equal(new DateOnly(2026, 1, 6), result.Points.First().Date);
        Assert.Equal(20, result.Records.Count);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(100001, 20)]
    [InlineData(1, 101)]
    public async Task InvalidPageDoesNotReadTheAnimal(int page, int size)
    {
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => Service.GetAsync(animal.Id, new GrowthPageRequest(page, size)));
        repository.Verify(r => r.GetAsync<Animal>(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EmptyHistoryDoesNotInventWeights() =>
        Assert.Empty((await Service.GetAsync(animal.Id)).Points);

    [Fact]
    public async Task DailyGainUsesElapsedDaysAndKeepsNegativeChange()
    {
        Add(1, 100); Add(11, 112); Add(16, 109);
        var result = await Service.GetAsync(animal.Id);
        Assert.Null(result.Points[0].DailyGainKg);
        Assert.Equal(1.2m, result.Points[1].DailyGainKg);
        Assert.Equal(-0.6m, result.Points[2].DailyGainKg);
    }

    [Fact]
    public async Task SameDayObservationsStayInHistoryAndLatestDrivesCurve()
    {
        Add(1, 100);
        var earlier = Add(11, 112);
        var latest = Add(11, 114, 1);
        var result = await Service.GetAsync(animal.Id);
        Assert.Equal(3, result.Records.Count);
        Assert.Equal(2, result.Points.Count);
        Assert.Equal(latest.Id, result.Points[1].RecordId);
        Assert.Equal(1.4m, result.Points[1].DailyGainKg);
        Assert.False(result.Records.Single(r => r.Id == earlier.Id).UsedForCurve);
    }

    [Fact]
    public async Task EqualTimestampsUseIdForDeterministicSelection()
    {
        Add(1, 100, id: Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var second = Add(1, 110, id: Guid.Parse("00000000-0000-0000-0000-000000000002"));
        Assert.Equal(second.Id, (await Service.GetAsync(animal.Id)).Points.Single().RecordId);
    }

    [Fact]
    public async Task RecordsFromOtherAnimalOrFarmAreExcluded()
    {
        Add(1, 100); var otherAnimal = Add(2, 300); otherAnimal.AnimalId = Guid.NewGuid();
        var otherFarm = Add(3, 400); otherFarm.FarmId = Guid.NewGuid();
        Assert.Single((await Service.GetAsync(animal.Id)).Records);
    }

    [Fact]
    public async Task UnassignedAnimalIsHiddenBeforeReadingHistory()
    {
        farms.Setup(f => f.CanAccessAsync(animal.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.GetAsync(animal.Id));
        repository.Verify(r => r.ListAsync(It.IsAny<Expression<Func<WeightRecord, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
