using Core.Domain.Livestock;
using Infrastructure.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests;

public sealed class AnimalWeightReaderTests
{
    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("weight-" + Guid.NewGuid())
            .Options);

    [Fact]
    public async Task GetLatestReturnsMostRecentRecordOrNull()
    {
        await using var db = CreateContext();
        var animalId = await SeedAsync(db);
        var reader = new AnimalWeightReader(db);

        var latest = await reader.GetLatestAsync(animalId);

        Assert.NotNull(latest);
        Assert.Equal(540m, latest!.WeightKg);
        Assert.Equal(3.8m, latest.BodyConditionScore);

        Assert.Null(await reader.GetLatestAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetLatestForReturnsOneEntryPerAnimalAndIgnoresUnknown()
    {
        await using var db = CreateContext();
        var animalId = await SeedAsync(db);
        var reader = new AnimalWeightReader(db);

        var latestByAnimal = await reader.GetLatestForAsync(new[] { animalId, Guid.NewGuid() });

        Assert.Single(latestByAnimal);
        Assert.True(latestByAnimal.ContainsKey(animalId));
        Assert.Equal(540m, latestByAnimal[animalId].WeightKg);
    }

    [Fact]
    public async Task GetLatestForEmptyReturnsEmpty()
    {
        await using var db = CreateContext();
        var reader = new AnimalWeightReader(db);

        var latestByAnimal = await reader.GetLatestForAsync(Array.Empty<Guid>());

        Assert.Empty(latestByAnimal);
    }

    [Fact]
    public async Task SameDayAndTimestampTieUsesSameIdentifierInBothReaders()
    {
        await using var db = CreateContext();
        var animalId = await SeedAsync(db);
        var animal = await db.Animals.FindAsync(animalId);
        var first = new WeightRecord(Guid.Parse("00000000-0000-0000-0000-000000000001")) { AnimalId = animalId, FarmId = animal!.FarmId, Date = new DateOnly(2026, 4, 1), WeightKg = 550 };
        var second = new WeightRecord(Guid.Parse("00000000-0000-0000-0000-000000000002")) { AnimalId = animalId, FarmId = animal.FarmId, Date = first.Date, WeightKg = 555 };
        db.AddRange(first, second);
        var timestamp = DateTime.UtcNow;
        db.Entry(first).Property(r => r.CreatedAt).CurrentValue = timestamp;
        db.Entry(second).Property(r => r.CreatedAt).CurrentValue = timestamp;
        await db.SaveChangesAsync();
        var reader = new AnimalWeightReader(db);
        Assert.Equal(555m, (await reader.GetLatestAsync(animalId))!.WeightKg);
        Assert.Equal(555m, (await reader.GetLatestForAsync([animalId]))[animalId].WeightKg);
    }

    private static async Task<Guid> SeedAsync(AppDbContext db)
    {
        var farm = new Farm { Name = "Finca Test", Code = "T" };
        var species = new Species { Name = "Bovino", Code = "BO", Purpose = ProductivePurpose.Meat };

        var animal = new Animal
        {
            Farm = farm,
            Species = species,
            InternalTag = "A-1",
            Sex = Sex.Male,
            Status = AnimalStatus.Active,
            Origin = AnimalOrigin.Born,
            Purpose = ProductivePurpose.Meat,
            HealthStatus = HealthStatus.Healthy
        };

        animal.WeightRecords.Add(new WeightRecord { FarmId = farm.Id, Animal = animal, Date = new DateOnly(2026, 1, 1), WeightKg = 500m, BodyConditionScore = 3.2m });
        animal.WeightRecords.Add(new WeightRecord { FarmId = farm.Id, Animal = animal, Date = new DateOnly(2026, 3, 1), WeightKg = 540m, BodyConditionScore = 3.8m });

        db.Animals.Add(animal);
        await db.SaveChangesAsync();

        return animal.Id;
    }
}
