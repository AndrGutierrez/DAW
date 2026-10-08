using Core.Application.Management;
namespace UnitTests;
public sealed class PaddockPlanValidationTests
{
    [Theory]
    [InlineData(0, 0, 100, 100, true)]
    [InlineData(10, 15, 35, 40, true)]
    [InlineData(-1, 0, 20, 20, false)]
    [InlineData(80, 0, 21, 20, false)]
    [InlineData(0, 90, 20, 11, false)]
    [InlineData(0, 0, 0, 20, false)]
    public void GeometryMustFitThePlan(decimal x, decimal y, decimal width, decimal height, bool valid) => Assert.Equal(valid, new PaddockRequestValidator().Validate(new PaddockRequest(Guid.NewGuid(), "North", MapX: x, MapY: y, MapWidth: width, MapHeight: height)).IsValid);
    [Fact]
    public void PartialGeometryIsRejected() => Assert.False(new PaddockRequestValidator().Validate(new PaddockRequest(Guid.NewGuid(), "North", MapX: 10)).IsValid);
    [Fact]
    public void UnconfiguredPlanRemainsValid() => Assert.True(new PaddockRequestValidator().Validate(new PaddockRequest(Guid.NewGuid(), "North")).IsValid);
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3651)]
    public void StayLimitMustBeOperationallyBounded(int days) => Assert.False(new PaddockRequestValidator().Validate(new PaddockRequest(Guid.NewGuid(), "North", MaxStayDays: days)).IsValid);
}
