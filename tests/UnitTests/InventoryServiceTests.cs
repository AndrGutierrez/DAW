using System.Linq.Expressions;
using Core.Application.Livestock;
using Core.Application.Management;
using Core.Application.Operations;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;
using Moq;
namespace UnitTests;
public sealed class InventoryServiceTests
{
    private readonly Mock<IManagementRepository> repository = new(MockBehavior.Strict);
    private readonly Mock<IFarmAccess> farms = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> user = new(MockBehavior.Strict);
    private readonly FarmInventory inventory = new() { FarmId = Guid.NewGuid(), ProductId = Guid.NewGuid(), Stock = 20.1234m };
    private readonly Guid author = Guid.NewGuid();
    private InventoryService Service => new(repository.Object, farms.Object, user.Object, Mock.Of<IOperationsReader>());
    private StockRequest Request => new(Guid.NewGuid(), StockMovementType.Out, 2.1234m, inventory.Stock, " Animal supplies ");
    public InventoryServiceTests()
    {
        repository.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<CareSubmission<StockRecord>>>>(), It.IsAny<CancellationToken>())).Returns((Func<Task<CareSubmission<StockRecord>>> action, CancellationToken _) => action());
        repository.Setup(r => r.GetAsync<FarmInventory>(inventory.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(inventory);
        farms.Setup(f => f.CanAccessAsync(inventory.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(r => r.GetAsync<StockMovement>(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>())).ReturnsAsync((StockMovement?)null);
        repository.Setup(r => r.GetAsync<Product>(inventory.ProductId, false, It.IsAny<CancellationToken>())).ReturnsAsync(new Product { IsActive = true });
        repository.Setup(r => r.GetAsync<Farm>(inventory.FarmId, false, It.IsAny<CancellationToken>())).ReturnsAsync(new Farm { IsActive = true });
        user.SetupGet(u => u.UserId).Returns(author);
    }
    [Fact]
    public async Task FirstWithdrawalKeepsExactOpeningBalanceAndAuthorAndWritesAtomically()
    {
        var saved = new List<StockMovement>(); var request = Request;
        repository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<StockMovement, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repository.Setup(r => r.Add(It.IsAny<StockMovement>())).Callback<StockMovement>(saved.Add);
        repository.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await Service.RecordAsync(inventory.Id, request);
        Assert.False(result.Replayed); Assert.Equal(18m, inventory.Stock); Assert.Equal(2, saved.Count);
        Assert.Equal(20.1234m, saved.Single(m => m.ReferenceType == "OpeningBalance").Quantity);
        var actual = saved.Single(m => m.Id == request.SubmissionId); Assert.Equal(author, actual.UserId); Assert.Equal("Animal supplies", actual.Reason); Assert.Equal(inventory.Id, actual.ReferenceId);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task ReplayDoesNotApplyQuantityTwiceEvenWithStaleExpectedBalance()
    {
        var request = Request; var existing = new StockMovement(request.SubmissionId) { FarmId = inventory.FarmId, ProductId = inventory.ProductId, UserId = author, Type = request.Type, Quantity = request.Quantity, Reason = request.Reason.Trim(), ReferenceType = "Inventory", ReferenceId = inventory.Id };
        repository.Setup(r => r.GetAsync<StockMovement>(existing.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(existing); inventory.Stock = 10;
        Assert.True((await Service.RecordAsync(inventory.Id, request)).Replayed); Assert.Equal(10, inventory.Stock);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task AnotherAuthorsSubmissionCannotBeReplayed()
    {
        var q = Request; repository.Setup(r => r.GetAsync<StockMovement>(q.SubmissionId, false, It.IsAny<CancellationToken>())).ReturnsAsync(new StockMovement(q.SubmissionId) { UserId = Guid.NewGuid() });
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(inventory.Id, q));
    }
    [Fact]
    public async Task StaleBalanceRejectsWithoutWriting()
    {
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(inventory.Id, Request with { ExpectedStock = 1 }));
        repository.Verify(r => r.Add(It.IsAny<StockMovement>()), Times.Never);
    }
    [Fact]
    public async Task InsufficientStockRejectsWithoutMutatingBalance()
    {
        await Assert.ThrowsAsync<ConflictException>(() => Service.RecordAsync(inventory.Id, Request with { Quantity = 30 })); Assert.Equal(20.1234m, inventory.Stock);
    }
    [Fact]
    public async Task InaccessibleInventoryIsHiddenBeforeSubmissionLookup()
    {
        farms.Setup(f => f.CanAccessAsync(inventory.FarmId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.RecordAsync(inventory.Id, Request));
        repository.Verify(r => r.GetAsync<StockMovement>(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task AnimalFromAnotherFarmIsRejected()
    {
        var id = Guid.NewGuid(); repository.Setup(r => r.GetAsync<Animal>(id, false, It.IsAny<CancellationToken>())).ReturnsAsync(new Animal { FarmId = Guid.NewGuid() });
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.RecordAsync(inventory.Id, Request with { AnimalId = id }));
    }
    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(0.00001)]
    public async Task InvalidQuantityDoesNotReadInventory(decimal quantity)
    {
        await Assert.ThrowsAsync<ValidationException>(() => Service.RecordAsync(inventory.Id, Request with { Quantity = quantity }));
        repository.Verify(r => r.GetAsync<FarmInventory>(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task IncomingStockCannotClaimAnimalConsumption()
    {
        await Assert.ThrowsAsync<ValidationException>(() => Service.RecordAsync(inventory.Id, Request with { Type = StockMovementType.In, AnimalId = Guid.NewGuid() }));
    }
}
