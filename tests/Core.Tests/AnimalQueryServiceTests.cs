using Core.Domain.Livestock;
using Core.Application.Security;
using Infrastructure.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests;

public sealed class AnimalQueryServiceTests
{
    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("animal-query-" + Guid.NewGuid())
            .Options);

    [Fact]
    public async Task ListProjectsRelevantInformationIncludingBirthDateAndLatestWeight()
    {
        await using var db = CreateContext();
        var (animalId, birthDate) = await SeedAnimalAsync(db);
        var service = new AnimalQueryService(db, new AnimalWeightReader(db), new AllFarmsAccess(db));

        var items = await service.ListAsync();

        var item = Assert.Single(items);
        Assert.Equal(animalId, item.Id);
        Assert.Equal("DEMO-001", item.InternalTag);
        Assert.Equal("Toro Bravo", item.Name);
        Assert.Equal("Bovino", item.Species);
        Assert.Equal("Brahman", item.Breed);
        Assert.Equal("Male", item.Sex);
        Assert.Equal("Active", item.Status);
        Assert.Equal("Healthy", item.HealthStatus);
        Assert.Equal(birthDate, item.BirthDate);
        Assert.Equal(520m, item.CurrentWeightKg);
        Assert.Equal("Engorde", item.Lot);
        Assert.Equal("Finca Test", item.Farm);
        Assert.NotEqual(Guid.Empty, item.FarmId);
        Assert.NotEqual(Guid.Empty, item.SpeciesId);
    }

    [Fact]
    public async Task GetReturnsDetailWithLatestWeightFromReader()
    {
        await using var db = CreateContext();
        var (animalId, _) = await SeedAnimalAsync(db);
        var service = new AnimalQueryService(db, new AnimalWeightReader(db), new AllFarmsAccess(db));

        var detail = await service.GetAsync(animalId);

        Assert.Equal(animalId, detail.Id);
        Assert.Equal("OF-DEMO-001", detail.OfficialId);
        Assert.Equal(520m, detail.CurrentWeightKg);
        Assert.Equal(3.5m, detail.BodyConditionScore);
        Assert.Equal("Engorde", detail.Lot);
        Assert.NotEqual(Guid.Empty, detail.FarmId);
        Assert.NotEqual(Guid.Empty, detail.SpeciesId);
        Assert.NotNull(detail.BreedId);
        Assert.NotNull(detail.LotId);
        Assert.Empty(detail.Photos);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ListStaleReturnsOnlyAnimalsOlderThanCutoff()
    {
        await using var db = CreateContext();
        var (freshId, _) = await SeedAnimalAsync(db);

        var farm = new Farm { Name = "Finca Vieja", Code = "V" };
        var species = new Species { Name = "Ovino", Code = "OV", Purpose = ProductivePurpose.Wool };
        db.Animals.Add(new Animal
        {
            Farm = farm,
            Species = species,
            InternalTag = "OLD-1",
            Sex = Sex.Female,
            Status = AnimalStatus.Active,
            Origin = AnimalOrigin.Born,
            Purpose = ProductivePurpose.Wool,
            HealthStatus = HealthStatus.Healthy,
            UpdatedAt = DateTime.UtcNow.AddDays(-90)
        });
        await db.SaveChangesAsync();

        var service = new AnimalQueryService(db, new AnimalWeightReader(db), new AllFarmsAccess(db));

        var stale = await service.ListStaleAsync(30);

        var item = Assert.Single(stale);
        Assert.Equal("OLD-1", item.InternalTag);
        Assert.DoesNotContain(stale, candidate => candidate.Id == freshId);
    }

    private static async Task<(Guid AnimalId, DateOnly BirthDate)> SeedAnimalAsync(AppDbContext db)
    {
        var farm = new Farm { Name = "Finca Test", Code = "T" };
        var species = new Species { Name = "Bovino", Code = "BO", Purpose = ProductivePurpose.DualPurpose };
        var breed = new Breed { Name = "Brahman", Species = species, Purpose = ProductivePurpose.Meat };
        var lot = new Lot { Farm = farm, Species = species, Name = "Engorde", Purpose = ProductivePurpose.Meat };
        var birthDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-30);

        var animal = new Animal
        {
            Farm = farm,
            Species = species,
            Breed = breed,
            Lot = lot,
            InternalTag = "DEMO-001",
            OfficialId = "OF-DEMO-001",
            Name = "Toro Bravo",
            Sex = Sex.Male,
            BirthDate = birthDate,
            Status = AnimalStatus.Active,
            Origin = AnimalOrigin.Born,
            Purpose = ProductivePurpose.Meat,
            HealthStatus = HealthStatus.Healthy
        };

        animal.WeightRecords.Add(new WeightRecord
        {
            FarmId = farm.Id,
            Animal = animal,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            WeightKg = 520m,
            BodyConditionScore = 3.5m
        });

        db.Animals.Add(animal);
        await db.SaveChangesAsync();

        return (animal.Id, birthDate);
    }

    private sealed class AllFarmsAccess(AppDbContext db) : IFarmAccess
    {
        public async Task<bool> CanAccessAsync(Guid farmId, CancellationToken cancellationToken = default) =>
            await db.Farms.AsNoTracking().AnyAsync(farm => farm.Id == farmId, cancellationToken);

        public async Task<IReadOnlyCollection<Guid>> GetAccessibleFarmIdsAsync(CancellationToken cancellationToken = default) =>
            await db.Farms.AsNoTracking().Select(farm => farm.Id).ToListAsync(cancellationToken);
    }
}
