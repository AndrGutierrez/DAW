using Core.Application.Cattle;
using Core.Domain.Cattle;
using Infrastructure.Cattle;

namespace Core.Tests;

public sealed class DomainAndApplicationTests
{
    [Fact]
    public void EntitiesHaveGuidIdentityUtcCreationAndHerdRelationship()
    {
        var before = DateTime.UtcNow;
        var herd = new Herd("North Pasture");
        var animal = new Animal("C-001", "Brahman", herd);
        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, herd.Id);
        Assert.NotEqual(Guid.Empty, animal.Id);
        Assert.NotEqual(herd.Id, animal.Id);
        Assert.Equal(DateTimeKind.Utc, herd.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, animal.CreatedAt.Kind);
        Assert.InRange(herd.CreatedAt, before, after);
        Assert.InRange(animal.CreatedAt, before, after);
        Assert.Same(herd, animal.Herd);
        Assert.Equal(herd.Id, animal.HerdId);
        Assert.Equal(AnimalHealthStatus.Healthy, animal.HealthStatus);
        animal.ChangeHealthStatus(AnimalHealthStatus.UnderObservation);
        Assert.Equal(AnimalHealthStatus.UnderObservation, animal.HealthStatus);
    }

    [Fact]
    public void CatalogRejectsDuplicateEarTagsRegardlessOfCase()
    {
        var catalog = CreateCatalog();
        var herd = catalog.CreateHerd("North Pasture", null);

        var animal = catalog.RegisterAnimal("c-001", "Brahman", herd.Id, null);

        Assert.Equal("C-001", animal.EarTag);
        Assert.Throws<InvalidOperationException>(() =>
            catalog.RegisterAnimal("C-001", "Holstein", herd.Id, null));
        Assert.Single(catalog.ListAnimals());
    }

    [Fact]
    public void CatalogRejectsUnknownHerdAndFutureBirthDate()
    {
        var catalog = CreateCatalog();

        Assert.Throws<KeyNotFoundException>(() =>
            catalog.RegisterAnimal("C-002", "Brahman", Guid.NewGuid(), null));

        var herd = catalog.CreateHerd("South Pasture", null);
        Assert.Throws<ArgumentException>(() =>
            catalog.RegisterAnimal("C-003", "Brahman", herd.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))));
    }

    private static CattleCatalogService CreateCatalog() =>
        new(
            new InMemoryCattleRepository(new InMemoryCattleStore()),
            new AnimalTagNormalizer(),
            new AnimalRegistrationValidator());
}
