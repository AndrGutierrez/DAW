using System.Linq.Expressions;
using Core.Application.Livestock;
using Core.Application.Management;
using Core.Domain.Livestock;
using Moq;

namespace UnitTests;

public sealed class AnimalLocationPolicyTests
{
    private readonly Mock<IManagementRepository> repository = new(MockBehavior.Strict);
    private readonly Animal animal = new() { FarmId = Guid.NewGuid(), SpeciesId = Guid.NewGuid() };
    private readonly Paddock paddock = new() { Capacity = 2 };
    private AnimalLocationPolicy Policy => new(repository.Object);
    public AnimalLocationPolicyTests()
    {
        paddock.FarmId = animal.FarmId;
        repository.Setup(r => r.GetAsync<Paddock>(paddock.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(paddock);
    }
    [Fact]
    public async Task CapacityCountsOnlyOtherActiveResidents()
    {
        var animals = new[] {
            animal,
            new Animal { PaddockId = paddock.Id, Status = AnimalStatus.Active },
            new Animal { PaddockId = paddock.Id, Status = AnimalStatus.Sold },
            new Animal { PaddockId = Guid.NewGuid(), Status = AnimalStatus.Active }
        };
        animal.PaddockId = paddock.Id;
        repository.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Animal, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Expression<Func<Animal, bool>> predicate, CancellationToken _) => Task.FromResult(animals.Count(predicate.Compile())));
        await Policy.CheckAsync(animal, paddock.Id, null);
        repository.Verify(r => r.CountAsync(It.IsAny<Expression<Func<Animal, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task FullOrOverfilledDestinationRejectsArrival(int residents)
    {
        repository.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Animal, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(residents);
        await Assert.ThrowsAsync<ConflictException>(() => Policy.CheckAsync(animal, paddock.Id, null));
    }
    [Fact]
    public async Task UnconfiguredCapacityDoesNotInventALimit()
    {
        paddock.Capacity = null;
        await Policy.CheckAsync(animal, paddock.Id, null);
        repository.Verify(r => r.CountAsync(It.IsAny<Expression<Func<Animal, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Theory]
    [InlineData(AnimalStatus.Sold)]
    [InlineData(AnimalStatus.Dead)]
    [InlineData(AnimalStatus.Transferred)]
    [InlineData(AnimalStatus.Lost)]
    public async Task InactiveAnimalDoesNotConsumeCapacity(AnimalStatus status)
    {
        animal.Status = status;
        await Policy.CheckAsync(animal, paddock.Id, null);
        repository.Verify(r => r.CountAsync(It.IsAny<Expression<Func<Animal, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task InactivePaddockIsRejectedBeforeCounting()
    {
        paddock.IsActive = false;
        await Assert.ThrowsAsync<ConflictException>(() => Policy.CheckAsync(animal, paddock.Id, null));
    }
    [Fact]
    public async Task CrossFarmPaddockIsRejectedBeforeCounting()
    {
        paddock.FarmId = Guid.NewGuid();
        await Assert.ThrowsAsync<ConflictException>(() => Policy.CheckAsync(animal, paddock.Id, null));
    }
    [Fact]
    public async Task LotMustMatchSpeciesAndFarm()
    {
        var lot = new Lot { FarmId = animal.FarmId, SpeciesId = Guid.NewGuid() };
        repository.Setup(r => r.GetAsync<Lot>(lot.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(lot);
        await Assert.ThrowsAsync<ConflictException>(() => Policy.CheckAsync(animal, null, lot.Id));
    }
}
