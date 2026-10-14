using Core.Application.Operations;
using Core.Application.Security;
using Moq;
namespace UnitTests;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task AReadOnlyHealthRoleCannotReceiveInventoryOrGrowthDetails()
    {
        var id = Guid.NewGuid(); var farm = Guid.NewGuid(); var actor = new Mock<ICurrentUser>(); actor.SetupGet(x => x.UserId).Returns(id);
        var access = new Mock<IFarmAccess>(); access.Setup(x => x.GetAccessibleFarmIdsAsync(default)).ReturnsAsync(new[] { farm });
        var permissions = new Mock<IPermissionChecker>();
        foreach (var name in new[] { "animals.list", "animals.get", "clinical.list" }) permissions.Setup(x => x.HasPermissionAsync(id, name, default)).ReturnsAsync(true);
        var reader = new Mock<INotificationReader>(MockBehavior.Strict);
        reader.Setup(x => x.ReadAsync(It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { farm })), new NotificationVisibility(false, true, false, false), default))
            .ReturnsAsync(new NotificationSnapshot([], 0, DateTime.UtcNow));
        Assert.Empty((await new NotificationService(actor.Object, permissions.Object, access.Object, reader.Object).GetAsync()).Items);
        reader.VerifyAll();
    }
    [Fact]
    public async Task InventoryRequiresBothInventoryAndProductPermissions()
    {
        var id = Guid.NewGuid(); var actor = new Mock<ICurrentUser>(); actor.SetupGet(x => x.UserId).Returns(id);
        var permissions = new Mock<IPermissionChecker>(); permissions.Setup(x => x.HasPermissionAsync(id, "inventory.list", default)).ReturnsAsync(true);
        var access = new Mock<IFarmAccess>(); access.Setup(x => x.GetAccessibleFarmIdsAsync(default)).ReturnsAsync(Array.Empty<Guid>());
        var reader = new Mock<INotificationReader>(); reader.Setup(x => x.ReadAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.Is<NotificationVisibility>(v => !v.Inventory), default)).ReturnsAsync(new NotificationSnapshot([], 0, DateTime.UtcNow));
        Assert.NotNull(await new NotificationService(actor.Object, permissions.Object, access.Object, reader.Object).GetAsync());
        reader.VerifyAll();
    }
    [Fact]
    public async Task AnonymousAccessStopsBeforeReadingAnyFarmData()
    {
        var reader = new Mock<INotificationReader>(MockBehavior.Strict);
        var service = new NotificationService(Mock.Of<ICurrentUser>(), Mock.Of<IPermissionChecker>(), Mock.Of<IFarmAccess>(), reader.Object);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAsync());
        reader.VerifyNoOtherCalls();
    }
}
