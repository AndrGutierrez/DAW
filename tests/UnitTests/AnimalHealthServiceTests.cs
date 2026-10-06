using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;
using Moq;

namespace UnitTests;

public sealed class AnimalHealthServiceTests
{
    private readonly Mock<IManagementRepository> repository = new(MockBehavior.Strict);
    private readonly Mock<IFarmAccess> farms = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> user = new(MockBehavior.Strict);
    private readonly Animal animal = new() { FarmId = Guid.NewGuid(), Status = AnimalStatus.Active, HealthStatus = HealthStatus.Healthy };
    private AnimalHealthService Service => new(repository.Object, farms.Object, new HealthUpdateRequestValidator(), user.Object);

    public AnimalHealthServiceTests()
    {
        repository.Setup(value => value.ExecuteWriteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<bool>> action, CancellationToken _) => action());
        repository.Setup(value => value.GetAsync<Animal>(animal.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(animal);
        farms.Setup(value => value.CanAccessAsync(animal.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(value => value.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task StatusChangeRecordsPreviousStateReasonAndAuthor()
    {
        animal.UpdatedAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        user.SetupGet(value => value.UserId).Returns(userId);
        HealthStatusChange? history = null;
        repository.Setup(value => value.Add(It.IsAny<HealthStatusChange>())).Callback<HealthStatusChange>(value => history = value);

        await Service.UpdateAsync(animal.Id, new HealthUpdateRequest(HealthStatus.InTreatment, "Veterinary assessment"));

        Assert.Equal(HealthStatus.InTreatment, animal.HealthStatus);
        Assert.True(animal.UpdatedAt > new DateTime(2020, 1, 1));
        Assert.NotNull(history);
        Assert.Equal(HealthStatus.Healthy, history.PreviousStatus);
        Assert.Equal(HealthStatus.InTreatment, history.NewStatus);
        Assert.Equal(animal.Id, history.AnimalId);
        Assert.Equal(animal.FarmId, history.FarmId);
        Assert.Equal(userId, history.UserId);
        Assert.Equal("Veterinary assessment", history.Reason);
        repository.Verify(value => value.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnchangedStatusDoesNotCreateDuplicateHistory()
    {
        await Service.UpdateAsync(animal.Id, new HealthUpdateRequest(HealthStatus.Healthy));
        repository.Verify(value => value.Add(It.IsAny<HealthStatusChange>()), Times.Never);
        user.VerifyGet(value => value.UserId, Times.Never);
    }

    [Fact]
    public async Task UnassignedFarmIsHiddenAndNeverWritten()
    {
        farms.Setup(value => value.CanAccessAsync(animal.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.UpdateAsync(animal.Id, new HealthUpdateRequest(HealthStatus.Quarantine)));
        Assert.Equal(HealthStatus.Healthy, animal.HealthStatus);
        repository.Verify(value => value.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(value => value.Add(It.IsAny<HealthStatusChange>()), Times.Never);
    }

    [Theory]
    [InlineData(AnimalStatus.Sold)]
    [InlineData(AnimalStatus.Dead)]
    [InlineData(AnimalStatus.Transferred)]
    [InlineData(AnimalStatus.Lost)]
    public async Task InactiveAnimalCannotChangeHealthStatus(AnimalStatus status)
    {
        animal.Status = status;
        await Assert.ThrowsAsync<ConflictException>(() => Service.UpdateAsync(animal.Id, new HealthUpdateRequest(HealthStatus.Quarantine)));
        Assert.Equal(HealthStatus.Healthy, animal.HealthStatus);
        repository.Verify(value => value.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingAnimalDoesNotReachAuthorizationOrPersistence()
    {
        repository.Setup(value => value.GetAsync<Animal>(animal.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync((Animal?)null);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.UpdateAsync(animal.Id, new HealthUpdateRequest(HealthStatus.Critical)));
        farms.Verify(value => value.CanAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(value => value.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvalidStatusIsRejectedBeforeReadingAnyAnimal()
    {
        await Assert.ThrowsAsync<ValidationException>(() => Service.UpdateAsync(animal.Id, new HealthUpdateRequest((HealthStatus)999)));
        repository.Verify(value => value.GetAsync<Animal>(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(value => value.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
