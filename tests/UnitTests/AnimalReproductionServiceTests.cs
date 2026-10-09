using System.Linq.Expressions;
using Core.Application.Livestock;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;
using Moq;
namespace UnitTests;
public sealed class AnimalReproductionServiceTests
{
    private readonly Mock<IManagementRepository> repo = new(MockBehavior.Strict);
    private readonly Mock<IFarmAccess> farms = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> user = new(MockBehavior.Strict);
    private readonly Animal dam = new() { FarmId = Guid.NewGuid(), SpeciesId = Guid.NewGuid(), Sex = Sex.Female, BirthDate = new(2020, 1, 1) };
    private readonly Guid author = Guid.NewGuid();
    private readonly List<Animal> animals = [];
    private readonly List<ReproductiveEvent> events = [];
    private AnimalReproductionService Service => new(repo.Object, farms.Object, user.Object, new ReproductiveRequestValidator(), new CarePageRequestValidator());
    public AnimalReproductionServiceTests()
    {
        animals.Add(dam); user.SetupGet(u => u.UserId).Returns(author);
        farms.Setup(f => f.CanAccessAsync(dam.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<CareSubmission<ReproductiveRequest>>>>(), It.IsAny<CancellationToken>())).Returns((Func<Task<CareSubmission<ReproductiveRequest>>> action, CancellationToken _) => action());
        repo.Setup(r => r.GetAsync<Animal>(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid id, bool _, CancellationToken _) => animals.SingleOrDefault(a => a.Id == id));
        repo.Setup(r => r.GetAsync<ReproductiveEvent>(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>())).ReturnsAsync((Guid id, bool _, CancellationToken _) => events.SingleOrDefault(e => e.Id == id));
        repo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<ReproductiveEvent, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expression<Func<ReproductiveEvent, bool>> filter, CancellationToken _) => events.Count(filter.Compile()));
        repo.Setup(r => r.PageAsync<ReproductiveEvent, DateOnly>(It.IsAny<Expression<Func<ReproductiveEvent, bool>>>(), It.IsAny<Expression<Func<ReproductiveEvent, DateOnly>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expression<Func<ReproductiveEvent, bool>> filter, Expression<Func<ReproductiveEvent, DateOnly>> order, int skip, int take, CancellationToken _) => events.Where(filter.Compile()).OrderByDescending(order.Compile()).ThenByDescending(e => e.CreatedAt).Skip(skip).Take(take).ToArray());
        repo.Setup(r => r.Add(It.IsAny<ReproductiveEvent>())).Callback<ReproductiveEvent>(events.Add);
        repo.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }
    private ReproductiveRequest Request(ReproductiveEventKind kind) => kind switch
    {
        ReproductiveEventKind.Heat => new(Guid.NewGuid(), kind, new(2026, 1, 10), "  Observation  ", Method: "Visual"),
        ReproductiveEventKind.PregnancyCheck => new(Guid.NewGuid(), kind, new(2026, 1, 10), Result: PregnancyResult.Positive, ExpectedCalvingDate: new(2026, 9, 1)),
        ReproductiveEventKind.Calving => new(Guid.NewGuid(), kind, new(2026, 1, 10), OffspringCount: 2, StillbornCount: 1),
        ReproductiveEventKind.Weaning => new(Guid.NewGuid(), kind, new(2026, 1, 10), WeightKg: 100),
        ReproductiveEventKind.Abortion => new(Guid.NewGuid(), kind, new(2026, 1, 10), Reason: "Recorded cause"),
        _ => new(Guid.NewGuid(), kind, new(2026, 1, 10))
    };
    [Theory]
    [InlineData(ReproductiveEventKind.Heat)] [InlineData(ReproductiveEventKind.Mating)] [InlineData(ReproductiveEventKind.Insemination)]
    [InlineData(ReproductiveEventKind.PregnancyCheck)] [InlineData(ReproductiveEventKind.Calving)] [InlineData(ReproductiveEventKind.Weaning)] [InlineData(ReproductiveEventKind.Abortion)]
    public async Task SupportedEventsPreserveAuthorAndMatchingReplayDoesNotWriteAgain(ReproductiveEventKind kind)
    {
        var q = Request(kind); var result = await Service.RecordAsync(dam.Id, q); Assert.False(result.Replayed);
        var saved = Assert.Single(events); Assert.Equal(dam.Id, saved.DamId); Assert.Equal(dam.FarmId, saved.FarmId); Assert.Equal(author, saved.UserId);
        Assert.Equal(q with { Notes = q.Notes?.Trim() }, AnimalReproductionService.Read(saved));
        Assert.True((await Service.RecordAsync(dam.Id, q)).Replayed); repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Theory] [InlineData("farm")] [InlineData("species")] [InlineData("sex")] [InlineData("birth")]
    public async Task InvalidSireCannotPersistAService(string mismatch)
    {
        var sire = new Animal { FarmId = dam.FarmId, SpeciesId = dam.SpeciesId, Sex = Sex.Male, BirthDate = new(2019, 1, 1) };
        if (mismatch == "farm") sire.FarmId = Guid.NewGuid(); if (mismatch == "species") sire.SpeciesId = Guid.NewGuid();
        if (mismatch == "sex") sire.Sex = Sex.Female; if (mismatch == "birth") sire.BirthDate = new(2026, 1, 10); animals.Add(sire);
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(dam.Id, Request(ReproductiveEventKind.Mating) with { SireId = sire.Id }));
        Assert.Empty(events); repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task MatchingSireIsAcceptedAndNonexistentSireIsRejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Service.RecordAsync(dam.Id, Request(ReproductiveEventKind.Insemination) with { SireId = Guid.NewGuid() }));
        var sire = new Animal { FarmId = dam.FarmId, SpeciesId = dam.SpeciesId, Sex = Sex.Male, BirthDate = new(2019, 1, 1) }; animals.Add(sire);
        await Service.RecordAsync(dam.Id, Request(ReproductiveEventKind.Insemination) with { SireId = sire.Id });
        Assert.Equal(sire.Id, Assert.IsType<Insemination>(Assert.Single(events)).SireId);
    }
    [Theory] [InlineData("dam")] [InlineData("farm")] [InlineData("species")] [InlineData("birth")]
    public async Task WeaningRequiresOffspringToBelongToTheDamAndEventDate(string mismatch)
    {
        var child = new Animal { DamId = dam.Id, FarmId = dam.FarmId, SpeciesId = dam.SpeciesId, BirthDate = new(2025, 1, 1) };
        if (mismatch == "dam") child.DamId = Guid.NewGuid(); if (mismatch == "farm") child.FarmId = Guid.NewGuid();
        if (mismatch == "species") child.SpeciesId = Guid.NewGuid(); if (mismatch == "birth") child.BirthDate = new(2026, 1, 11); animals.Add(child);
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(dam.Id, Request(ReproductiveEventKind.Weaning) with { OffspringId = child.Id }));
        Assert.Empty(events);
    }
    [Fact]
    public async Task MatchingOffspringIsAcceptedAndMissingOffspringIsRejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Service.RecordAsync(dam.Id, Request(ReproductiveEventKind.Weaning) with { OffspringId = Guid.NewGuid() }));
        var child = new Animal { DamId = dam.Id, FarmId = dam.FarmId, SpeciesId = dam.SpeciesId, BirthDate = new(2025, 1, 1) }; animals.Add(child);
        await Service.RecordAsync(dam.Id, Request(ReproductiveEventKind.Weaning) with { OffspringId = child.Id }); Assert.Single(events);
    }
    [Theory] [InlineData("Unknown", -1)] [InlineData("Pregnant", 0)] [InlineData("NotPregnant", 1)] [InlineData("Uncertain", 2)] [InlineData("Calved", 3)] [InlineData("Aborted", 4)]
    public async Task StatusUsesLatestSignificantEventInsideTheAnimalFarm(string state, int kind)
    {
        ReproductiveEvent? record = kind switch { 0 => new PregnancyCheck { Result = PregnancyResult.Positive }, 1 => new PregnancyCheck { Result = PregnancyResult.Negative }, 2 => new PregnancyCheck { Result = PregnancyResult.Uncertain }, 3 => new Calving { OffspringCount = 1 }, 4 => new Abortion(), _ => null };
        if (record != null) { record.DamId = dam.Id; record.FarmId = dam.FarmId; record.Date = new(2026, 1, 10); events.Add(record); }
        events.Add(new PregnancyCheck { DamId = dam.Id, FarmId = Guid.NewGuid(), Date = new(2026, 1, 20), Result = PregnancyResult.Positive });
        var result = await Service.GetAsync(dam.Id, new()); Assert.Equal(state, result.Status.State); Assert.Equal(record == null ? 0 : 1, result.History.Total);
        repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task MaleHasNoReproductiveStatusAndEventBeforeBirthIsRejected()
    {
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(dam.Id, Request(ReproductiveEventKind.Heat) with { Date = new(2019, 1, 1) }));
        dam.Sex = Sex.Male; Assert.Equal("NotApplicable", (await Service.GetAsync(dam.Id, new())).Status.State);
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(dam.Id, Request(ReproductiveEventKind.Heat))); Assert.Empty(events);
    }
    [Fact]
    public async Task UnassignedFarmIsHiddenAndDoesNotReadReproductiveHistory()
    {
        farms.Setup(f => f.CanAccessAsync(dam.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.GetAsync(dam.Id, new()));
        repo.Verify(r => r.CountAsync(It.IsAny<Expression<Func<ReproductiveEvent, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
