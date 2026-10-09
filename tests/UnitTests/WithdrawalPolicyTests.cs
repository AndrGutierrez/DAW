using System.Linq.Expressions;
using Core.Application.Livestock;
using Core.Application.Management;
using Core.Domain.Livestock;
using Moq;

namespace UnitTests;

public sealed class WithdrawalPolicyTests
{
    private readonly Mock<IManagementRepository> repository = new();
    private readonly Guid animal = Guid.NewGuid();
    private readonly List<Treatment> treatments = [];
    private WithdrawalPolicy Policy => new(repository.Object);
    public WithdrawalPolicyTests()
    {
        repository.Setup(r => r.ListAsync<Treatment>(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Treatment, bool>> filter, CancellationToken _) => treatments.Where(filter.Compile()).ToList());
    }
    [Theory]
    [InlineData(1, true)] [InlineData(3, true)] [InlineData(5, true)] [InlineData(6, false)]
    public async Task WithdrawalCoversCourseAndInclusiveFinalDay(int day, bool blocked)
    {
        treatments.Add(new() { AnimalId = animal, Date = new(2026, 1, 1), EndDate = new(2026, 1, 3), WithdrawalDays = 2, WithdrawalEndDate = new(2026, 1, 5) });
        var status = await Policy.GetAsync(animal, new(2026, 1, day));
        Assert.Equal(blocked, status.Blocked);
        if (blocked) Assert.Equal(new DateOnly(2026, 1, 6), status.ReleaseDate);
    }
    [Fact]
    public async Task ZeroDaysStillRestrictsAdministrationDate()
    {
        treatments.Add(new() { AnimalId = animal, Date = new(2026, 1, 1), EndDate = new(2026, 1, 1), WithdrawalDays = 0, WithdrawalEndDate = new(2026, 1, 1) });
        Assert.True((await Policy.GetAsync(animal, new(2026, 1, 1))).Blocked);
        Assert.False((await Policy.GetAsync(animal, new(2026, 1, 2))).Blocked);
    }
    [Fact]
    public async Task OverlappingCoursesUseLatestRelease()
    {
        treatments.Add(new() { AnimalId = animal, Date = new(2026, 1, 1), EndDate = new(2026, 1, 1), WithdrawalDays = 2 });
        treatments.Add(new() { AnimalId = animal, Date = new(2026, 1, 2), EndDate = new(2026, 1, 4), WithdrawalDays = 5 });
        var status = await Policy.GetAsync(animal, new(2026, 1, 3));
        Assert.Equal(2, status.Treatments);
        Assert.Equal(new DateOnly(2026, 1, 10), status.ReleaseDate);
    }
    [Fact]
    public async Task UnknownLegacyWithdrawalFailsClosedWithoutInventingReleaseDate()
    {
        treatments.Add(new() { AnimalId = animal, Date = new(2026, 1, 1) });
        var status = await Policy.GetAsync(animal, new(2026, 2, 1));
        Assert.True(status.Blocked);
        Assert.Null(status.ReleaseDate);
        await Assert.ThrowsAsync<ConflictException>(() => Policy.CheckAsync(animal, new(2026, 2, 1), default));
    }
    [Fact]
    public async Task FutureCourseAndOtherAnimalDoNotBlockEarlierProduction()
    {
        treatments.Add(new() { AnimalId = animal, Date = new(2026, 2, 1) });
        treatments.Add(new() { AnimalId = Guid.NewGuid(), Date = new(2026, 1, 1) });
        Assert.False((await Policy.GetAsync(animal, new(2026, 1, 10))).Blocked);
    }
    [Fact]
    public async Task ShortLegacyEndDateCannotOverrideLongerSnapshot()
    {
        treatments.Add(new() { AnimalId = animal, Date = new(2026, 1, 1), EndDate = new(2026, 1, 2), WithdrawalDays = 5, WithdrawalEndDate = new(2026, 1, 3) });
        Assert.Equal(new DateOnly(2026, 1, 8), (await Policy.GetAsync(animal, new(2026, 1, 6))).ReleaseDate);
    }
}
