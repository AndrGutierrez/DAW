using System.Linq.Expressions;
using Core.Application.Livestock;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using Moq;
namespace UnitTests;
public sealed class AnimalCatalogServiceTests
{
    private readonly Mock<IManagementRepository> repo = new(MockBehavior.Strict);
    private readonly Mock<IFarmAccess> access = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> user = new(MockBehavior.Strict);
    private readonly Farm farm = new() { Name = "Farm", Code = "A" };
    private readonly Species species = new() { Code = "BO" };
    private readonly Animal animal = new() { InternalTag = "A-1", Sex = Sex.Female, BirthDate = new(2020, 1, 1) };
    private readonly List<Animal> animals = [];
    private readonly List<WeightRecord> weights = [];
    private readonly List<HealthEvent> health = [];
    private readonly List<ReproductiveEvent> reproduction = [];
    private readonly List<AnimalProduction> production = [];
    private readonly List<AnimalPhoto> photos = [];
    private readonly List<HealthStatusChange> changes = [];
    private readonly List<StockMovement> stock = [];
    private readonly List<AnimalMovement> moves = [];
    private readonly Guid author = Guid.NewGuid();
    private AnimalDefinition Definition => new(repo.Object, user.Object, new(repo.Object));
    private CrudService<Animal, AnimalRequest> Service => new(repo.Object, Definition, new AnimalRequestValidator(), access.Object);
    private AnimalRequest Request => Definition.Read(animal);
    public AnimalCatalogServiceTests()
    {
        animal.FarmId = farm.Id; animal.SpeciesId = species.Id; animals.Add(animal);
        user.SetupGet(u => u.UserId).Returns(author);
        access.Setup(a => a.CanAccessAsync(farm.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        access.Setup(a => a.GetAccessibleFarmIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([farm.Id]);
        repo.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<ResourceResult<AnimalRequest>>>>(), It.IsAny<CancellationToken>())).Returns((Func<Task<ResourceResult<AnimalRequest>>> action, CancellationToken _) => action());
        repo.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>())).Returns((Func<Task<bool>> action, CancellationToken _) => action());
        repo.Setup(r => r.GetAsync<Farm>(farm.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(farm);
        repo.Setup(r => r.GetAsync<Species>(species.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(species);
        repo.Setup(r => r.GetAsync<Animal>(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid id, bool _, CancellationToken _) => animals.SingleOrDefault(a => a.Id == id));
        repo.Setup(r => r.ListAsync(It.IsAny<Expression<Func<Animal, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expression<Func<Animal, bool>> filter, CancellationToken _) => animals.Where(filter.Compile()).ToArray());
        Exists(animals); Exists(weights); Exists(health); Exists(reproduction); Exists(production); Exists(photos); Exists(changes); Exists(stock); Exists(moves);
        repo.Setup(r => r.Add(It.IsAny<Animal>())).Callback<Animal>(animals.Add);
        repo.Setup(r => r.Add(It.IsAny<HealthStatusChange>())).Callback<HealthStatusChange>(changes.Add);
        repo.Setup(r => r.Add(It.IsAny<AnimalMovement>())).Callback<AnimalMovement>(moves.Add);
        repo.Setup(r => r.Remove(It.IsAny<Animal>())).Callback<Animal>(a => animals.Remove(a));
        repo.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }
    private void Exists<T>(List<T> rows) where T : Core.Domain.Common.BaseEntity =>
        repo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expression<Func<T, bool>> filter, CancellationToken _) => rows.Any(filter.Compile()));
    [Fact]
    public async Task CreateNormalizesIdentityAndPreservesOptionalAnimalData()
    {
        var result = await Service.CreateAsync(Request with { InternalTag = " new-1 ", OfficialId = " official-1 ", Rfid = " rfid-1 ", Name = " Aurora ", Color = " Red ", Markings = " White ", Notes = " Healthy ", BirthWeightKg = 30.12m });
        Assert.Equal("NEW-1", result.Data.InternalTag); Assert.Equal("OFFICIAL-1", result.Data.OfficialId); Assert.Equal("RFID-1", result.Data.Rfid);
        Assert.Equal("Aurora", result.Data.Name); Assert.Equal(30.12m, result.Data.BirthWeightKg); Assert.Equal("Healthy", result.Data.Notes);
        repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Theory] [InlineData("tag")] [InlineData("official")] [InlineData("rfid")]
    public async Task DuplicateIdentityCannotPersistEvenWithDifferentCaseOrSpaces(string identity)
    {
        animal.OfficialId = "OFFICIAL-1"; animal.Rfid = "RFID-1";
        var q = Request with { InternalTag = identity == "tag" ? " a-1 " : "new-1", OfficialId = identity == "official" ? " official-1 " : null, Rfid = identity == "rfid" ? " rfid-1 " : null };
        await Assert.ThrowsAsync<ConflictException>(() => Service.CreateAsync(q)); Assert.Single(animals);
        repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Theory] [InlineData(true)] [InlineData(false)]
    public async Task InactiveFarmOrSpeciesCannotReceiveNewAnimals(bool inactiveFarm)
    {
        if (inactiveFarm) farm.IsActive = false; else species.IsActive = false;
        await Assert.ThrowsAsync<ConflictException>(() => Service.CreateAsync(Request with { InternalTag = "new-1" })); Assert.Single(animals);
    }
    [Theory] [InlineData("farm")] [InlineData("species")] [InlineData("sex")] [InlineData("age")] [InlineData("cycle")]
    public async Task InvalidParentsCannotChangeGenealogy(string mismatch)
    {
        var parent = new Animal { FarmId = farm.Id, SpeciesId = species.Id, Sex = Sex.Female, BirthDate = new(2018, 1, 1) };
        if (mismatch == "farm") parent.FarmId = Guid.NewGuid(); if (mismatch == "species") parent.SpeciesId = Guid.NewGuid();
        if (mismatch == "sex") parent.Sex = Sex.Male; if (mismatch == "age") parent.BirthDate = animal.BirthDate;
        if (mismatch == "cycle") parent.DamId = animal.Id; animals.Add(parent);
        await Assert.ThrowsAsync<ConflictException>(() => Service.UpdateAsync(animal.Id, Request with { DamId = parent.Id })); Assert.Null(animal.DamId);
        repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task ValidOlderParentsAreSavedAndScopeHidesOtherFarms()
    {
        var dam = new Animal { FarmId = farm.Id, SpeciesId = species.Id, Sex = Sex.Female, BirthDate = new(2018, 1, 1) };
        var sire = new Animal { FarmId = farm.Id, SpeciesId = species.Id, Sex = Sex.Male, BirthDate = new(2017, 1, 1) };
        animals.AddRange([dam, sire, new() { FarmId = Guid.NewGuid() }]);
        var result = await Service.UpdateAsync(animal.Id, Request with { DamId = dam.Id, SireId = sire.Id }); Assert.Equal(dam.Id, result.Data.DamId); Assert.Equal(sire.Id, result.Data.SireId);
        Assert.Equal(3, (await Service.ListAsync()).Count); Assert.Equal(animal.Id, (await Service.GetAsync(animal.Id)).Id);
    }
    [Fact]
    public async Task BirthDateCannotFollowAnExistingWeighing()
    {
        weights.Add(new() { AnimalId = animal.Id, Date = new(2026, 1, 1) });
        await Assert.ThrowsAsync<ConflictException>(() => Service.UpdateAsync(animal.Id, Request with { BirthDate = new(2026, 1, 10) })); Assert.Equal(new DateOnly(2020, 1, 1), animal.BirthDate);
    }
    [Theory] [InlineData("health")] [InlineData("reproduction")] [InlineData("production")] [InlineData("offspring")]
    public async Task DependentHistoryPreventsRewritingAnimalBirthDate(string history)
    {
        if (history == "health") health.Add(new Treatment { AnimalId = animal.Id });
        if (history == "reproduction") reproduction.Add(new PregnancyCheck { DamId = animal.Id });
        if (history == "production") production.Add(new() { AnimalId = animal.Id });
        if (history == "offspring") animals.Add(new() { DamId = animal.Id });
        await Assert.ThrowsAsync<ConflictException>(() => Service.UpdateAsync(animal.Id, Request with { BirthDate = new(2019, 1, 1) }));
        repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task ASlaughteredAnimalCannotBecomeActiveAgain()
    {
        animal.Status = AnimalStatus.Dead; production.Add(new() { AnimalId = animal.Id, Method = ProductionMethod.Slaughter });
        await Assert.ThrowsAsync<ConflictException>(() => Service.UpdateAsync(animal.Id, Request with { Status = AnimalStatus.Active })); Assert.Equal(AnimalStatus.Dead, animal.Status);
    }
    [Fact]
    public async Task HealthChangeRecordsPreviousStatusAndAuthor()
    {
        await Service.UpdateAsync(animal.Id, Request with { HealthStatus = HealthStatus.InTreatment }); var change = Assert.Single(changes);
        Assert.Equal(HealthStatus.Healthy, change.PreviousStatus); Assert.Equal(HealthStatus.InTreatment, change.NewStatus); Assert.Equal(author, change.UserId);
    }
    [Theory] [InlineData("supply")] [InlineData("photo")] [InlineData("health")] [InlineData("movement")]
    public async Task TraceableHistoryPreventsHardDeletion(string history)
    {
        if (history == "movement") moves.Add(new() { AnimalId = animal.Id });
        if (history == "supply") stock.Add(new() { FarmId = farm.Id, ReferenceType = "Animal", ReferenceId = animal.Id });
        if (history == "photo") photos.Add(new() { AnimalId = animal.Id }); if (history == "health") changes.Add(new() { AnimalId = animal.Id });
        await Assert.ThrowsAsync<ConflictException>(() => Service.DeleteAsync(animal.Id)); Assert.Contains(animal, animals);
        repo.Verify(r => r.Remove(It.IsAny<Animal>()), Times.Never); repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task UnreferencedAnimalCanBeDeletedOnce()
    { await Service.DeleteAsync(animal.Id); Assert.Empty(animals); repo.Verify(r => r.Remove(animal), Times.Once); repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once); }
}
