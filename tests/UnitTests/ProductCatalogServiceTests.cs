using System.Linq.Expressions;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;
using Moq;

namespace UnitTests;

public sealed class ProductCatalogServiceTests
{
    private static readonly Guid CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private Guid ProductId => product.Id;
    private readonly Mock<IManagementRepository> repository = new(MockBehavior.Strict);
    private readonly Mock<IFarmAccess> farms = new(MockBehavior.Strict);
    private readonly Product product = new() { SKU = "FEED-01", Name = "Feed", CategoryId = CategoryId, Price = 12.50m, CostPrice = 8.25m, Unit = MeasurementUnit.Unit };
    private ProductRequest Request => new("feed-01", " Feed updated ", CategoryId, 15.75m, 9.50m, MeasurementUnit.Unit, " Farm supply ");
    private CrudService<Product, ProductRequest> Service => new(repository.Object, new ProductDefinition(repository.Object), new ProductRequestValidator(), farms.Object);

    public ProductCatalogServiceTests()
    {
        repository.Setup(r => r.ExecuteWriteAsync(It.IsAny<Func<Task<ResourceResult<ProductRequest>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<ResourceResult<ProductRequest>>> action, CancellationToken _) => action());
        repository.Setup(r => r.GetAsync<InventoryCategory>(CategoryId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryCategory { IsActive = true });
        repository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
    }

    [Fact]
    public async Task ExistingProductIsMappedWithoutTrackingOrWriting()
    {
        repository.Setup(r => r.GetAsync<Product>(ProductId, false, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        var result = await Service.GetAsync(ProductId);
        Assert.Equal(ProductId, result.Id);
        Assert.Equal(product.Price, result.Data.Price);
        Assert.Equal(product.CategoryId, result.Data.CategoryId);
        repository.Verify(r => r.GetAsync<Product>(ProductId, false, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingProductThrowsAfterExactlyOneLookup()
    {
        repository.Setup(r => r.GetAsync<Product>(ProductId, false, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.GetAsync(ProductId));
        repository.Verify(r => r.GetAsync<Product>(ProductId, false, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreationPreservesDecimalPricesAndNormalizesCatalogText()
    {
        Product? saved = null;
        repository.Setup(r => r.Add(It.IsAny<Product>())).Callback<Product>(entity => saved = entity);
        repository.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await Service.CreateAsync(Request);
        Assert.NotNull(saved);
        Assert.Equal(saved.Id, result.Id);
        Assert.Equal("FEED-01", saved.SKU);
        Assert.Equal("Feed updated", saved.Name);
        Assert.Equal("Farm supply", saved.Brand);
        Assert.Equal(15.75m, saved.Price);
        Assert.Equal(9.50m, saved.CostPrice);
        Assert.Equal(CategoryId, saved.CategoryId);
        repository.Verify(r => r.Add(It.IsAny<Product>()), Times.Once);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(12.345)]
    public async Task InvalidPriceIsRejectedBeforeCatalogLookupsOrWrites(decimal price)
    {
        var failure = await Assert.ThrowsAsync<ValidationException>(() => Service.CreateAsync(Request with { Price = price }));
        Assert.Contains(failure.Errors, error => error.PropertyName == nameof(ProductRequest.Price));
        repository.Verify(r => r.GetAsync<InventoryCategory>(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingCategoryRejectsCreationWithoutWriting()
    {
        repository.Setup(r => r.GetAsync<InventoryCategory>(CategoryId, false, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryCategory?)null);
        await Assert.ThrowsAsync<ArgumentException>(() => Service.CreateAsync(Request));
        repository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InactiveCategoryRejectsCreationWithoutWriting()
    {
        repository.Setup(r => r.GetAsync<InventoryCategory>(CategoryId, false, It.IsAny<CancellationToken>())).ReturnsAsync(new InventoryCategory { IsActive = false });
        await Assert.ThrowsAsync<ConflictException>(() => Service.CreateAsync(Request));
        repository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DuplicateNormalizedSkuRejectsCreationWithoutWriting()
    {
        repository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Expression<Func<Product, bool>> predicate, CancellationToken _) => Task.FromResult(predicate.Compile()(product)));
        await Assert.ThrowsAsync<ConflictException>(() => Service.CreateAsync(Request));
        repository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateKeepsOwnSkuAndUnitWithoutQueryingStock()
    {
        repository.Setup(r => r.GetAsync<Product>(ProductId, true, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        repository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Expression<Func<Product, bool>> predicate, CancellationToken _) => Task.FromResult(predicate.Compile()(product)));
        repository.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await Service.UpdateAsync(ProductId, Request);
        Assert.Equal(ProductId, result.Id);
        Assert.Equal(15.75m, product.Price);
        Assert.Equal("Feed updated", product.Name);
        repository.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmInventory, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StockedProductCannotChangeUnitOrPartiallyUpdateOtherFields()
    {
        repository.Setup(r => r.GetAsync<Product>(ProductId, true, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        repository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmInventory, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Expression<Func<FarmInventory, bool>> predicate, CancellationToken _) => Task.FromResult(predicate.Compile()(new FarmInventory { ProductId = ProductId })));
        await Assert.ThrowsAsync<ConflictException>(() => Service.UpdateAsync(ProductId, Request with { Unit = MeasurementUnit.Kilogram }));
        Assert.Equal(MeasurementUnit.Unit, product.Unit);
        Assert.Equal(12.50m, product.Price);
        Assert.Equal("Feed", product.Name);
        repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
