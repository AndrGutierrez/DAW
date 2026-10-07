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
}
