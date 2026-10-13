using Core.Application.Livestock;
using Core.Application.Management;
using Core.Application.Security;
using Moq;
namespace UnitTests;

public sealed class AnimalIdentificationServiceTests
{
    [Fact]
    public async Task UnauthorizedFarmIsHiddenBeforeLookup()
    {
        var reader = new Mock<IAnimalIdentificationReader>(MockBehavior.Strict);
        var service = new AnimalIdentificationService(Mock.Of<IFarmAccess>(), reader.Object, Mock.Of<IAnimalQueryService>());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.IdentifyAsync(Guid.NewGuid(), "ABC"));
        reader.VerifyNoOtherCalls();
    }
    [Fact]
    public async Task ALabelIsResolvedWithinTheChosenFarmAndAmbiguousIdentifiersAreRejected()
    {
        var farm = Guid.NewGuid(); var id = Guid.NewGuid(); var access = new Mock<IFarmAccess>(); access.Setup(x => x.CanAccessAsync(farm, default)).ReturnsAsync(true);
        var reader = new Mock<IAnimalIdentificationReader>(MockBehavior.Strict); reader.Setup(x => x.FindAsync(farm, id.ToString(), default)).ReturnsAsync(new[] { id, Guid.NewGuid() });
        var animals = new Mock<IAnimalQueryService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<ConflictException>(() => new AnimalIdentificationService(access.Object, reader.Object, animals.Object).IdentifyAsync(farm, "daw:animal:" + id));
        animals.VerifyNoOtherCalls(); reader.VerifyAll();
    }
    [Theory]
    [InlineData("")]
    [InlineData("daw:animal:invalid")]
    public async Task InvalidLabelCannotBeSentToTheReader(string label)
    {
        var farm = Guid.NewGuid(); var access = new Mock<IFarmAccess>(); access.Setup(x => x.CanAccessAsync(farm, default)).ReturnsAsync(true);
        var reader = new Mock<IAnimalIdentificationReader>(MockBehavior.Strict);
        await Assert.ThrowsAsync<ArgumentException>(() => new AnimalIdentificationService(access.Object, reader.Object, Mock.Of<IAnimalQueryService>()).IdentifyAsync(farm, label));
        reader.VerifyNoOtherCalls();
    }
}
