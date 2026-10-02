using Core.Application.Management;
using Core.Domain.Livestock;

namespace Core.Tests;

public sealed class DomainAndApplicationTests
{
    [Fact]
    public void EntitiesHaveGuidIdentityUtcCreationAndFarmRelationship()
    {
        var before = DateTime.UtcNow;
        var farm = new Farm
        {
            Name = "North Farm",
            Code = "NORTH"
        };
        var animal = new Animal
        {
            InternalTag = "C-001",
            Farm = farm,
            FarmId = farm.Id
        };
        Assert.NotEqual(Guid.Empty, farm.Id);
        Assert.NotEqual(Guid.Empty, animal.Id);
        Assert.NotEqual(farm.Id, animal.Id);
        Assert.Equal(DateTimeKind.Utc, animal.CreatedAt.Kind);
        Assert.InRange(animal.CreatedAt, before, DateTime.UtcNow);
        Assert.Same(farm, animal.Farm);
        Assert.Equal(HealthStatus.Healthy, animal.HealthStatus);
    }

    [Fact]
    public void AnimalRegistrationRejectsFutureBirthDatesAndUnknownEnumValues()
    {
        var request = new AnimalRequest(Guid.NewGuid(), Guid.NewGuid(), "C-001", Sex.Female, ProductivePurpose.Milk, BirthDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), HealthStatus: (HealthStatus)99);
        var result = new AnimalRequestValidator().Validate(request);
        Assert.Contains(result.Errors, x => x.PropertyName == "BirthDate");
        Assert.Contains(result.Errors, x => x.PropertyName == "HealthStatus");
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(0, 1)]
    public void ProductValidatorRejectsNonpositivePrices(decimal price, decimal cost)
    {
        var result = new ProductRequestValidator().Validate(new ProductRequest("FEED-1", "Feed", Guid.NewGuid(), price, cost, MeasurementUnit.Bag));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName is "Price" or "CostPrice");
    }

    [Fact]
    public void InventoryRequiresNonnegativeStockAndOrderedThresholds()
    {
        var result = new InventoryRequestValidator().Validate(new InventoryRequest(Guid.NewGuid(), Guid.NewGuid(), -2, 20, 10));
        Assert.Contains(result.Errors, x => x.PropertyName == "Stock");
        Assert.Contains(result.Errors, x => x.PropertyName == "MaxStock");
    }

    [Fact]
    public void ProductionRejectsMilkObtainedBySlaughterAndFractionalEggs()
    {
        var validator = new ProductionRequestValidator();
        var milk = new ProductionRequest(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), AnimalProductType.Milk, ProductionMethod.Slaughter, 20, MeasurementUnit.Liter, Guid.NewGuid());
        Assert.False(validator.Validate(milk).IsValid);
        Assert.False(validator.Validate(milk with { ProductType = AnimalProductType.Eggs, Method = ProductionMethod.Collection, Unit = MeasurementUnit.Unit, Quantity = 1.5m }).IsValid);
    }
}
