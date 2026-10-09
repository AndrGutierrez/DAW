using System.Linq.Expressions;
using Core.Application.Livestock;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;
using Moq;

namespace UnitTests;

public sealed class AnimalCareServiceTests
{
    private readonly Mock<IManagementRepository> repository = new();
    private readonly Mock<IFarmAccess> farms = new();
    private readonly Mock<ICurrentUser> user = new();
    private readonly Animal animal = new() { FarmId = Guid.NewGuid(), SpeciesId = Guid.NewGuid(), Sex = Sex.Female, Status = AnimalStatus.Active, BirthDate = new(2020, 1, 1) };
    private readonly Product product = new() { IsActive = true, WithdrawalDays = 5 };
    private readonly Guid userId = Guid.NewGuid();
    private readonly List<HealthEvent> clinical = [];
    private readonly List<ReproductiveEvent> reproductive = [];
    private readonly List<AnimalProduction> production = [];
    private ClinicalRequest Request => new(Guid.NewGuid(), ClinicalEventKind.Treatment, new(2026, 1, 1), "  Check  ", product.Id, 2, MedicationRoute.Oral, new(2026, 1, 3));
    private AnimalCareService Care => new(repository.Object, farms.Object, user.Object, new ClinicalRequestValidator(), new CarePageRequestValidator(), new(repository.Object));
    private AnimalReproductionService Reproduction => new(repository.Object, farms.Object, user.Object, new ReproductiveRequestValidator(), new CarePageRequestValidator());
    public AnimalCareServiceTests()
    {
        repository.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<CareSubmission<ClinicalRequest>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<CareSubmission<ClinicalRequest>>> action, CancellationToken _) => action());
        repository.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<CareSubmission<ReproductiveRequest>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<CareSubmission<ReproductiveRequest>>> action, CancellationToken _) => action());
        repository.Setup(r => r.GetAsync<Animal>(animal.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(animal);
        repository.Setup(r => r.GetAsync<Product>(product.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        repository.Setup(r => r.GetAsync<HealthEvent>(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, bool _, CancellationToken _) => clinical.SingleOrDefault(x => x.Id == id));
        repository.Setup(r => r.GetAsync<ReproductiveEvent>(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, bool _, CancellationToken _) => reproductive.SingleOrDefault(x => x.Id == id));
        repository.Setup(r => r.ExistsAsync<AnimalProduction>(It.IsAny<Expression<Func<AnimalProduction, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<AnimalProduction, bool>> filter, CancellationToken _) => production.Any(filter.Compile()));
        repository.Setup(r => r.Add(It.IsAny<HealthEvent>())).Callback<HealthEvent>(clinical.Add);
        repository.Setup(r => r.Add(It.IsAny<ReproductiveEvent>())).Callback<ReproductiveEvent>(reproductive.Add);
        repository.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        farms.Setup(f => f.CanAccessAsync(animal.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        user.SetupGet(u => u.UserId).Returns(userId);
    }
    [Fact]
    public async Task TreatmentSnapshotsCatalogAndUsesFinalAdministration()
    {
        var q = Request;
        var saved = await Care.RecordAsync(animal.Id, q);
        var record = Assert.IsType<Treatment>(Assert.Single(clinical));
        Assert.Equal(q.SubmissionId, saved.Id);
        Assert.Equal(5, record.WithdrawalDays);
        Assert.Equal(new DateOnly(2026, 1, 8), record.WithdrawalEndDate);
        Assert.Equal(animal.FarmId, record.FarmId);
        Assert.Equal(userId, record.UserId);
        Assert.Equal("Check", record.Notes);
        Assert.False(saved.Replayed);
    }
    [Fact]
    public async Task MatchingReplaySurvivesCatalogChangeAndNeverWritesAgain()
    {
        var q = Request;
        await Care.RecordAsync(animal.Id, q);
        product.WithdrawalDays = 10; animal.Status = AnimalStatus.Sold;
        Assert.True((await Care.RecordAsync(animal.Id, q)).Replayed);
        Assert.Single(clinical);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task IdentifierCannotBeReusedWithDifferentDose()
    {
        var q = Request;
        await Care.RecordAsync(animal.Id, q);
        await Assert.ThrowsAsync<ConflictException>(() => Care.RecordAsync(animal.Id, q with { Dose = 3 }));
        Assert.Single(clinical);
    }
    [Theory]
    [InlineData(0)] [InlineData(4)]
    public async Task OverrideCannotShortenCatalogWithdrawal(int days)
    {
        await Assert.ThrowsAsync<ConflictException>(() => Care.RecordAsync(animal.Id, Request with { WithdrawalDays = days }));
        Assert.Empty(clinical);
    }
    [Fact]
    public async Task UnknownProductWithdrawalNeedsExplicitPeriod()
    {
        product.WithdrawalDays = null;
        await Assert.ThrowsAsync<ConflictException>(() => Care.RecordAsync(animal.Id, Request));
        await Care.RecordAsync(animal.Id, Request with { WithdrawalDays = 7 });
        Assert.Equal(7, Assert.IsType<Treatment>(Assert.Single(clinical)).WithdrawalDays);
    }
    [Theory]
    [InlineData(ProductionMethod.Milking)] [InlineData(ProductionMethod.Slaughter)]
    public async Task BackdatedTreatmentCannotContradictProduction(ProductionMethod method)
    {
        production.Add(new() { AnimalId = animal.Id, Date = new(2026, 1, 8), Method = method });
        await Assert.ThrowsAsync<ConflictException>(() => Care.RecordAsync(animal.Id, Request));
        Assert.Empty(clinical);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task ProductionOutsideWithdrawalAllowsTreatment()
    {
        production.Add(new() { AnimalId = animal.Id, Date = new(2026, 1, 9), Method = ProductionMethod.Milking });
        await Care.RecordAsync(animal.Id, Request);
        Assert.Single(clinical);
    }
    [Theory]
    [InlineData(ClinicalEventKind.Vaccination)] [InlineData(ClinicalEventKind.Deworming)]
    public async Task OtherAdministrationCannotBypassProductWithdrawal(ClinicalEventKind kind)
    {
        await Assert.ThrowsAsync<ConflictException>(() => Care.RecordAsync(animal.Id, new(Guid.NewGuid(), kind, new(2026, 1, 1), ProductId: product.Id)));
        Assert.Empty(clinical);
    }
    [Fact]
    public async Task UnassignedFarmIsHiddenBeforeClinicalLookup()
    {
        farms.Setup(f => f.CanAccessAsync(animal.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Care.RecordAsync(animal.Id, Request));
        repository.Verify(r => r.GetAsync<HealthEvent>(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(clinical);
    }
    [Theory]
    [InlineData(AnimalStatus.Dead)] [InlineData(AnimalStatus.Sold)]
    public async Task InactiveAnimalCannotRegisterClinicalEvents(AnimalStatus status)
    {
        animal.Status = status;
        await Assert.ThrowsAsync<ConflictException>(() => Care.RecordAsync(animal.Id, Request));
        Assert.Empty(clinical);
    }
    [Fact]
    public async Task TreatmentCannotPrecedeBirth()
    {
        await Assert.ThrowsAsync<ConflictException>(() => Care.RecordAsync(animal.Id, Request with { Date = new(2019, 1, 1) }));
        Assert.Empty(clinical);
    }
    [Theory]
    [InlineData(-1)] [InlineData(3651)]
    public async Task InvalidWithdrawalIsRejectedBeforeReadingAnimal(int days)
    {
        await Assert.ThrowsAsync<ValidationException>(() => Care.RecordAsync(animal.Id, Request with { WithdrawalDays = days }));
        repository.Verify(r => r.GetAsync<Animal>(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task InvalidCourseEndCannotPersist()
    {
        await Assert.ThrowsAsync<ValidationException>(() => Care.RecordAsync(animal.Id, Request with { EndDate = new(2025, 12, 31) }));
        Assert.Empty(clinical);
    }
    [Fact]
    public async Task ReproductionRequiresFemaleAndMatchingSire()
    {
        var q = new ReproductiveRequest(Guid.NewGuid(), ReproductiveEventKind.Mating, new(2026, 1, 1), SireId: animal.Id);
        await Assert.ThrowsAsync<ConflictException>(() => Reproduction.RecordAsync(animal.Id, q));
        animal.Sex = Sex.Male;
        await Assert.ThrowsAsync<ConflictException>(() => Reproduction.RecordAsync(animal.Id, q with { SireId = null }));
        Assert.Empty(reproductive);
    }
    [Fact]
    public async Task CalvingCannotHaveMoreStillbornThanTotalOffspring()
    {
        await Assert.ThrowsAsync<ValidationException>(() => Reproduction.RecordAsync(animal.Id,
            new(Guid.NewGuid(), ReproductiveEventKind.Calving, new(2026, 1, 1), OffspringCount: 1, StillbornCount: 2)));
        Assert.Empty(reproductive);
    }
    [Fact]
    public async Task ReproductiveReplayPreservesAuthorAndDoesNotDuplicate()
    {
        var q = new ReproductiveRequest(Guid.NewGuid(), ReproductiveEventKind.PregnancyCheck, new(2026, 1, 1), Result: PregnancyResult.Positive, ExpectedCalvingDate: new(2026, 9, 1));
        await Reproduction.RecordAsync(animal.Id, q);
        Assert.True((await Reproduction.RecordAsync(animal.Id, q)).Replayed);
        Assert.Equal(userId, Assert.Single(reproductive).UserId);
        await Assert.ThrowsAsync<ConflictException>(() => Reproduction.RecordAsync(animal.Id, q with { Result = PregnancyResult.Negative, ExpectedCalvingDate = null }));
    }
}
