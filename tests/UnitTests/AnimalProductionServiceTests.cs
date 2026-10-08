using System.Linq.Expressions;
using Core.Application.Livestock;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;
using Moq;
namespace UnitTests;
public sealed class AnimalProductionServiceTests
{
    private readonly Mock<IManagementRepository> repo = new(MockBehavior.Strict);
    private readonly Mock<IFarmAccess> farms = new(MockBehavior.Strict);
    private readonly Animal animal = new() { FarmId = Guid.NewGuid(), SpeciesId = Guid.NewGuid(), Sex = Sex.Female, BirthDate = new(2020, 1, 1) };
    private readonly Species species = new() { Code = "BO" };
    private readonly List<AnimalProduction> records = [];
    private readonly List<Treatment> treatments = [];
    private readonly List<WeightRecord> weights = [];
    private AnimalProductionRequest Request => new(Guid.NewGuid(), new(2026, 1, 10), AnimalProductType.Milk, ProductionMethod.Milking, 5.1234m, MeasurementUnit.Liter, "  Morning  ");
    private AnimalProductionService Service => new(repo.Object, farms.Object, new ProductionDefinition(repo.Object), new ProductionRequestValidator(), new CarePageRequestValidator());
    public AnimalProductionServiceTests()
    {
        repo.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<CareSubmission<ProductionRequest>>>>(), It.IsAny<CancellationToken>())).Returns((Func<Task<CareSubmission<ProductionRequest>>> action, CancellationToken _) => action());
        repo.Setup(r => r.GetAsync<Animal>(animal.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(animal);
        repo.Setup(r => r.GetAsync<Species>(animal.SpeciesId, false, It.IsAny<CancellationToken>())).ReturnsAsync(species);
        repo.Setup(r => r.GetAsync<AnimalProduction>(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>())).ReturnsAsync((Guid id, bool _, CancellationToken _) => records.SingleOrDefault(r => r.Id == id));
        repo.Setup(r => r.ListAsync(It.IsAny<Expression<Func<AnimalProduction, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expression<Func<AnimalProduction, bool>> filter, CancellationToken _) => records.Where(filter.Compile()).ToArray());
        repo.Setup(r => r.ListAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expression<Func<Treatment, bool>> filter, CancellationToken _) => treatments.Where(filter.Compile()).ToArray());
        repo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AnimalProduction, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expression<Func<AnimalProduction, bool>> filter, CancellationToken _) => records.Any(filter.Compile()));
        repo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<WeightRecord, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expression<Func<WeightRecord, bool>> filter, CancellationToken _) => weights.Any(filter.Compile()));
        repo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<AnimalProduction, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expression<Func<AnimalProduction, bool>> filter, CancellationToken _) => records.Count(filter.Compile()));
        repo.Setup(r => r.PageAsync<AnimalProduction, DateOnly>(It.IsAny<Expression<Func<AnimalProduction, bool>>>(), It.IsAny<Expression<Func<AnimalProduction, DateOnly>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expression<Func<AnimalProduction, bool>> filter, Expression<Func<AnimalProduction, DateOnly>> order, int skip, int take, CancellationToken _) => records.Where(filter.Compile()).OrderByDescending(order.Compile()).Skip(skip).Take(take).ToArray());
        repo.Setup(r => r.Add(It.IsAny<AnimalProduction>())).Callback<AnimalProduction>(records.Add);
        repo.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        farms.Setup(f => f.CanAccessAsync(animal.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }
    [Fact]
    public async Task MilkKeepsPrecisionFarmOwnershipAndReplayDoesNotWriteAgain()
    {
        var q = Request; var result = await Service.RecordAsync(animal.Id, q);
        Assert.Equal(5.1234m, Assert.Single(records).Quantity); Assert.Equal(animal.FarmId, result.Data.FarmId); Assert.Equal("Morning", result.Data.Notes);
        Assert.True((await Service.RecordAsync(animal.Id, q)).Replayed);
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(animal.Id, q with { Quantity = 7 }));
        repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task MedicationWithdrawalBlocksMilkThroughItsFinalDay()
    {
        treatments.Add(new() { AnimalId = animal.Id, Date = new(2026, 1, 1), EndDate = new(2026, 1, 5), WithdrawalDays = 5 });
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(animal.Id, Request));
        Assert.Empty(records); repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        await Service.RecordAsync(animal.Id, Request with { Date = new(2026, 1, 11) }); Assert.Single(records);
    }
    [Theory] [InlineData(Sex.Male)]
    public async Task NonFemaleCannotBeMilked(Sex sex)
    {
        animal.Sex = sex; await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(animal.Id, Request));
        repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Theory] [InlineData("AV")] [InlineData("EQ")]
    public async Task UnsupportedMilkSpeciesCannotPersist(string code)
    {
        species.Code = code; await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(animal.Id, Request)); Assert.Empty(records);
    }
    [Fact]
    public async Task AnUnassignedFarmIsHiddenBeforeReadingProduction()
    {
        farms.Setup(f => f.CanAccessAsync(animal.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.RecordAsync(animal.Id, Request));
        repo.Verify(r => r.GetAsync<AnimalProduction>(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()), Times.Never);
    }
    [Theory] [InlineData(0)] [InlineData(-1)]
    public async Task InvalidYieldNeverWrites(decimal quantity)
    {
        await Assert.ThrowsAsync<ValidationException>(() => Service.RecordAsync(animal.Id, Request with { Quantity = quantity })); Assert.Empty(records);
    }
    [Fact]
    public async Task MilkCannotUseKilograms()
    { await Assert.ThrowsAsync<ValidationException>(() => Service.RecordAsync(animal.Id, Request with { Unit = MeasurementUnit.Kilogram })); Assert.Empty(records); }
    [Fact]
    public async Task SlaughterCannotPrecedeAnExistingLiveWeight()
    {
        weights.Add(new() { AnimalId = animal.Id, Date = new(2026, 1, 11) });
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(animal.Id, Request with { ProductType = AnimalProductType.Meat, Method = ProductionMethod.Slaughter, Unit = MeasurementUnit.Kilogram }));
        Assert.Equal(AnimalStatus.Active, animal.Status); Assert.Empty(records);
    }
    [Fact]
    public async Task SlaughterMarksAnimalDeadAndIsIrreversible()
    {
        var q = Request with { ProductType = AnimalProductType.Meat, Method = ProductionMethod.Slaughter, Unit = MeasurementUnit.Kilogram };
        await Service.RecordAsync(animal.Id, q); Assert.Equal(AnimalStatus.Dead, animal.Status);
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(animal.Id, q with { SubmissionId = Guid.NewGuid() }));
        await Assert.ThrowsAsync<ConflictException>(() => new ProductionDefinition(repo.Object).BeforeDeleteAsync(records[0], default));
        repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task ProductionHistoryPagesOnlyTheRequestedAnimalAndFarm()
    {
        records.AddRange([new() { FarmId = animal.FarmId, AnimalId = animal.Id, Date = new(2026, 1, 10) }, new() { FarmId = Guid.NewGuid(), AnimalId = animal.Id, Date = new(2026, 1, 11) }, new() { FarmId = animal.FarmId, AnimalId = Guid.NewGuid(), Date = new(2026, 1, 12) }]);
        var page = await Service.GetAsync(animal.Id, new(1, 10)); Assert.Equal(1, page.Total); Assert.Equal(records[0].Id, Assert.Single(page.Items).Id);
        repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
