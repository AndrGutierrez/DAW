using Core.Application.Livestock;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;
using Moq;

namespace UnitTests;

public sealed class AnimalWeighingServiceTests
{
    private readonly Mock<IManagementRepository> repository = new(MockBehavior.Strict);
    private readonly Mock<IFarmAccess> farms = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> user = new(MockBehavior.Strict);
    private readonly Mock<IResourceDefinition<WeightRecord, WeightRequest>> definition = new(MockBehavior.Strict);
    private readonly Animal animal = new() { FarmId = Guid.NewGuid(), Status = AnimalStatus.Active };
    private readonly Guid userId = Guid.NewGuid();
    private readonly AnimalWeighingRequest request = new(Guid.NewGuid(), new DateOnly(2026, 1, 10), 125, 3, " Check ");
    private AnimalWeighingService Service => new(repository.Object, farms.Object, user.Object, definition.Object, new WeightRequestValidator());
    private WeightRequest Data => new(animal.FarmId, animal.Id, request.Date, 125, 3, "Check");

    public AnimalWeighingServiceTests()
    {
        repository.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<AnimalWeighingResult>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<AnimalWeighingResult>> action, CancellationToken _) => action());
        repository.Setup(r => r.GetAsync<Animal>(animal.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(animal);
        farms.Setup(f => f.CanAccessAsync(animal.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(r => r.GetAsync<WeightRecord>(request.SubmissionId, false, It.IsAny<CancellationToken>())).ReturnsAsync((WeightRecord?)null);
        user.SetupGet(u => u.UserId).Returns(userId);
        definition.Setup(d => d.CheckAsync(It.IsAny<WeightRecord>(), Data, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        definition.Setup(d => d.Apply(It.IsAny<WeightRecord>(), Data));
        repository.Setup(r => r.Add(It.IsAny<WeightRecord>()));
        repository.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task NewSubmissionDerivesOwnershipAndPreservesId()
    {
        var result = await Service.RecordAsync(animal.Id, request);
        Assert.Equal(request.SubmissionId, result.Id);
        Assert.False(result.Replayed);
        Assert.Equal(Data, result.Data);
        repository.Verify(r => r.Add(It.Is<WeightRecord>(w => w.Id == request.SubmissionId)), Times.Once);
        definition.Verify(d => d.CheckAsync(It.IsAny<WeightRecord>(), Data, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MatchingReplayDoesNotWriteAgainEvenIfAnimalBecameInactive()
    {
        animal.Status = AnimalStatus.Sold;
        var existing = new WeightRecord(request.SubmissionId) { RecordedByUserId = userId };
        repository.Setup(r => r.GetAsync<WeightRecord>(request.SubmissionId, false, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        definition.Setup(d => d.Read(existing)).Returns(Data);
        Assert.True((await Service.RecordAsync(animal.Id, request)).Replayed);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IdentifierCollisionCannotOverwriteAnotherSubmission(bool differentAuthor)
    {
        var existing = new WeightRecord(request.SubmissionId) { RecordedByUserId = differentAuthor ? Guid.NewGuid() : userId };
        repository.Setup(r => r.GetAsync<WeightRecord>(request.SubmissionId, false, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        if (!differentAuthor) definition.Setup(d => d.Read(existing)).Returns(Data with { WeightKg = 200 });
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(animal.Id, request));
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(AnimalStatus.Sold)]
    [InlineData(AnimalStatus.Dead)]
    [InlineData(AnimalStatus.Transferred)]
    [InlineData(AnimalStatus.Lost)]
    public async Task InactiveAnimalsRejectNewWeighings(AnimalStatus status)
    {
        animal.Status = status;
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(animal.Id, request));
        repository.Verify(r => r.Add(It.IsAny<WeightRecord>()), Times.Never);
    }

    [Fact]
    public async Task UnassignedFarmDoesNotReadOrWriteWeights()
    {
        farms.Setup(f => f.CanAccessAsync(animal.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.RecordAsync(animal.Id, request));
        repository.Verify(r => r.GetAsync<WeightRecord>(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(12.345)]
    public async Task InvalidWeightNeverPersists(double weight)
    {
        await Assert.ThrowsAsync<ValidationException>(() => Service.RecordAsync(animal.Id, request with { WeightKg = (decimal)weight }));
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
