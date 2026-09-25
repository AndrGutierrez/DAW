using Core.Application.Assets;
using Core.Domain.Assets;
using Infrastructure.Assets;

namespace Core.Tests;

public sealed class DomainAndApplicationTests
{
    [Fact]
    public void EntitiesHaveGuidIdentityUtcCreationAndCategoryRelationship()
    {
        var before = DateTime.UtcNow;
        var category = new AssetCategory("Computers");
        var asset = new ITAsset("IT-001", "Laptop", category, "SN-123");
        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.NotEqual(Guid.Empty, asset.Id);
        Assert.NotEqual(category.Id, asset.Id);
        Assert.Equal(DateTimeKind.Utc, category.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, asset.CreatedAt.Kind);
        Assert.InRange(category.CreatedAt, before, after);
        Assert.InRange(asset.CreatedAt, before, after);
        Assert.Same(category, asset.Category);
        Assert.Equal(category.Id, asset.CategoryId);
        Assert.Equal(AssetStatus.InStock, asset.Status);
    }

    [Fact]
    public void CatalogRejectsDuplicateAssetTagsRegardlessOfCase()
    {
        var catalog = CreateCatalog();
        var category = catalog.CreateCategory("Computers", null);

        var asset = catalog.RegisterAsset("it-001", "Laptop", category.Id, null);

        Assert.Equal("IT-001", asset.AssetTag);
        Assert.Throws<InvalidOperationException>(() =>
            catalog.RegisterAsset("IT-001", "Other laptop", category.Id, null));
        Assert.Single(catalog.ListAssets());
    }

    [Fact]
    public void CatalogRejectsUnknownCategory()
    {
        var catalog = CreateCatalog();

        Assert.Throws<KeyNotFoundException>(() =>
            catalog.RegisterAsset("IT-002", "Monitor", Guid.NewGuid(), null));
    }

    private static AssetCatalogService CreateCatalog() =>
        new(new InMemoryAssetRepository(), new AssetTagNormalizer());
}
