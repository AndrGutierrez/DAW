using Core.Application.Livestock;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;
using Moq;

namespace UnitTests;

public sealed class AnimalMovementServiceTests
{
    private readonly Mock<IManagementRepository> repository = new(MockBehavior.Strict);
    private readonly Mock<IFarmAccess> farms = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> user = new(MockBehavior.Strict);
    private readonly Animal animal = new() { FarmId = Guid.NewGuid(), SpeciesId = Guid.NewGuid(), PaddockId = Guid.NewGuid(), LotId = Guid.NewGuid() };
    private readonly Guid author = Guid.NewGuid();
    private AnimalMovementService Service => new(repository.Object, farms.Object, user.Object, new AnimalLocationPolicy(repository.Object));
    private AnimalMovementRequest Request => new(Guid.NewGuid(), null, null, animal.PaddockId, animal.LotId, " Rotation ");
    public AnimalMovementServiceTests()
    {
        repository.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<CareSubmission<AnimalMovementRecord>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<CareSubmission<AnimalMovementRecord>>> action, CancellationToken _) => action());
        repository.Setup(r => r.GetAsync<Animal>(animal.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(animal);
        farms.Setup(f => f.CanAccessAsync(animal.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        user.SetupGet(u => u.UserId).Returns(author);
        repository.Setup(r => r.GetAsync<AnimalMovement>(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>())).ReturnsAsync((AnimalMovement?)null);
        repository.Setup(r => r.GetAsync<Farm>(animal.FarmId, false, It.IsAny<CancellationToken>())).ReturnsAsync(new Farm { IsActive = true });
    }
    [Fact]
    public async Task TransferKeepsSourceAuthorAndCurrentDateAndUpdatesAnimal()
    {
        var q = Request;
        AnimalMovement? saved = null;
        repository.Setup(r => r.Add(It.IsAny<AnimalMovement>())).Callback<AnimalMovement>(m => saved = m);
        repository.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await Service.RecordAsync(animal.Id, q);
        Assert.False(result.Replayed);
        Assert.NotNull(saved); Assert.Equal(q.SubmissionId, saved.Id); Assert.Equal(q.ExpectedFromPaddockId, saved.FromPaddockId);
        Assert.Equal(q.ExpectedFromLotId, saved.FromLotId); Assert.Equal("Rotation", saved.Reason); Assert.Equal(author, saved.UserId);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), saved.Date);
        Assert.Null(animal.PaddockId); Assert.Null(animal.LotId);
    }
    [Fact]
    public async Task ChangedSourceNeverMutatesOrWrites()
    {
        var q = Request with { ExpectedFromPaddockId = Guid.NewGuid() };
        var source = animal.PaddockId;
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(animal.Id, q));
        Assert.Equal(source, animal.PaddockId);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task NoChangeIsRejected()
    {
        var q = Request with { ToPaddockId = animal.PaddockId, ToLotId = animal.LotId };
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(animal.Id, q));
    }
    [Fact]
    public async Task ReplaySucceedsEvenAfterAnotherMovement()
    {
        var q = Request;
        var record = new AnimalMovement(q.SubmissionId) { AnimalId = animal.Id, FarmId = animal.FarmId, UserId = author,
            FromPaddockId = q.ExpectedFromPaddockId, FromLotId = q.ExpectedFromLotId, Reason = "Rotation" };
        repository.Setup(r => r.GetAsync<AnimalMovement>(q.SubmissionId, false, It.IsAny<CancellationToken>())).ReturnsAsync(record);
        animal.PaddockId = Guid.NewGuid();
        var result = await Service.RecordAsync(animal.Id, q);
        Assert.True(result.Replayed);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task ReplayCannotUseAnotherAuthorsIdentifier()
    {
        var q = Request;
        repository.Setup(r => r.GetAsync<AnimalMovement>(q.SubmissionId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AnimalMovement(q.SubmissionId) { AnimalId = animal.Id, FarmId = animal.FarmId, UserId = Guid.NewGuid() });
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(animal.Id, q));
    }
    [Fact]
    public async Task MissingFarmAssignmentHidesAnimal()
    {
        farms.Setup(f => f.CanAccessAsync(animal.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.RecordAsync(animal.Id, Request));
        repository.Verify(r => r.GetAsync<AnimalMovement>(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task InvalidBodyDoesNotReadAnimal()
    {
        await Assert.ThrowsAsync<ValidationException>(() => Service.RecordAsync(animal.Id, Request with { Reason = " " }));
        repository.Verify(r => r.GetAsync<Animal>(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task InactiveAnimalCannotMove()
    {
        animal.Status = AnimalStatus.Sold;
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(animal.Id, Request));
    }

    [Fact]
    public async Task BatchKeepsLotsAndWritesInsideOneTransaction()
    {
        var target = Guid.NewGuid();
        var second = new Animal { FarmId = animal.FarmId, SpeciesId = animal.SpeciesId, PaddockId = animal.PaddockId };
        repository.Setup(r => r.GetAsync<Animal>(animal.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(animal);
        repository.Setup(r => r.GetAsync<Animal>(second.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(second);
        repository.Setup(r => r.GetAsync<Animal>(second.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(second);
        repository.Setup(r => r.GetAsync<Paddock>(target, false, It.IsAny<CancellationToken>())).ReturnsAsync(new Paddock { FarmId = animal.FarmId, IsActive = true });
        repository.Setup(r => r.GetAsync<Lot>(animal.LotId!.Value, false, It.IsAny<CancellationToken>())).ReturnsAsync(new Lot { FarmId = animal.FarmId, SpeciesId = animal.SpeciesId, IsActive = true });
        repository.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<AnimalMovementBatchResult>>>(), It.IsAny<CancellationToken>())).Returns((Func<Task<AnimalMovementBatchResult>> action, CancellationToken _) => action());
        var saved = new List<AnimalMovement>();
        repository.Setup(r => r.Add(It.IsAny<AnimalMovement>())).Callback<AnimalMovement>(saved.Add);
        repository.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var originalLot = animal.LotId;
        var result = await Service.RecordBatchAsync(new(animal.FarmId, target, " Rotation ", [
            new(animal.Id, Guid.NewGuid(), animal.PaddockId, animal.LotId), new(second.Id, Guid.NewGuid(), second.PaddockId, null)]));
        Assert.Equal(2, result.Items.Count); Assert.False(result.Replayed);
        Assert.Equal(target, animal.PaddockId); Assert.Equal(target, second.PaddockId);
        Assert.Equal(originalLot, animal.LotId); Assert.Null(second.LotId);
        Assert.All(saved, record => { Assert.Equal(author, record.UserId); Assert.Equal("Rotation", record.Reason); });
        repository.Verify(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<AnimalMovementBatchResult>>>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<CareSubmission<AnimalMovementRecord>>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task MixedFarmBatchIsRejectedBeforeAnyWrites()
    {
        var second = new Animal { FarmId = Guid.NewGuid() };
        repository.Setup(r => r.GetAsync<Animal>(animal.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(animal);
        repository.Setup(r => r.GetAsync<Animal>(second.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(second);
        farms.Setup(f => f.CanAccessAsync(second.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<AnimalMovementBatchResult>>>(), It.IsAny<CancellationToken>())).Returns((Func<Task<AnimalMovementBatchResult>> action, CancellationToken _) => action());
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordBatchAsync(new(animal.FarmId, Guid.NewGuid(), "Rotation", [
            new(animal.Id, Guid.NewGuid(), animal.PaddockId, animal.LotId), new(second.Id, Guid.NewGuid(), null, null)])));
        repository.Verify(r => r.Add(It.IsAny<AnimalMovement>()), Times.Never);
    }
    [Fact]
    public async Task DuplicateBatchMembersAreRejectedBeforeReadingAnimals()
    {
        repository.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<AnimalMovementBatchResult>>>(), It.IsAny<CancellationToken>())).Returns((Func<Task<AnimalMovementBatchResult>> action, CancellationToken _) => action());
        var member = new AnimalMovementBatchItem(animal.Id, Guid.NewGuid(), animal.PaddockId, animal.LotId);
        await Assert.ThrowsAsync<ValidationException>(() => Service.RecordBatchAsync(new(animal.FarmId, Guid.NewGuid(), "Rotation", [member, member])));
        repository.Verify(r => r.GetAsync<Animal>(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task BatchReplayKeepsTheOriginalLotEvenIfTheAnimalLaterChangesGroup()
    {
        var target = Guid.NewGuid(); var sourceLot = animal.LotId; var sourcePaddock = animal.PaddockId; var submission = Guid.NewGuid();
        var record = new AnimalMovement(submission) { FarmId = animal.FarmId, AnimalId = animal.Id, UserId = author,
            FromPaddockId = sourcePaddock, FromLotId = sourceLot, ToPaddockId = target, ToLotId = sourceLot, Reason = "Rotation" };
        repository.Setup(r => r.GetAsync<Animal>(animal.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(animal);
        repository.Setup(r => r.GetAsync<AnimalMovement>(submission, false, It.IsAny<CancellationToken>())).ReturnsAsync(record);
        repository.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<AnimalMovementBatchResult>>>(), It.IsAny<CancellationToken>())).Returns((Func<Task<AnimalMovementBatchResult>> action, CancellationToken _) => action());
        animal.LotId = Guid.NewGuid();
        var result = await Service.RecordBatchAsync(new(animal.FarmId, target, "Rotation", [new(animal.Id, submission, sourcePaddock, sourceLot)]));
        Assert.True(result.Replayed); Assert.Equal(sourceLot, Assert.Single(result.Items).Data.ToLotId);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
