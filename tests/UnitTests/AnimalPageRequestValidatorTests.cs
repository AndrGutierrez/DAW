using Core.Application.Livestock;
using Core.Domain.Livestock;

namespace UnitTests;

public sealed class AnimalPageRequestValidatorTests
{
    private readonly AnimalPageRequestValidator validator = new();

    [Fact]
    public void DefaultsAreValid() => Assert.True(validator.Validate(new AnimalPageRequest()).IsValid);

    [Theory]
    [InlineData(0, 12)]
    [InlineData(-1, 12)]
    [InlineData(100001, 12)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void InvalidPaginationIsRejected(int page, int pageSize) =>
        Assert.False(validator.Validate(new AnimalPageRequest(page, pageSize)).IsValid);

    [Fact]
    public void OversizedSearchIsRejected() =>
        Assert.False(validator.Validate(new AnimalPageRequest(Search: new string('a', 101))).IsValid);

    [Fact]
    public void UnknownStatusIsRejected() =>
        Assert.False(validator.Validate(new AnimalPageRequest(Status: (AnimalStatus)999)).IsValid);

    [Fact]
    public void EmptyFarmIsRejected() =>
        Assert.False(validator.Validate(new AnimalPageRequest(FarmId: Guid.Empty)).IsValid);
}
