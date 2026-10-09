using System.Net;
using System.Net.Http.Json;
using Core.Application.Livestock;
using Core.Application.Management;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Tests;

public sealed partial class ManagementIntegrationTests
{
    [Fact]
    public async Task GrowthHistoryPagesExposeScopedTotalsAndKeepTheSameCurve()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var animal = await Create(client, "animals", new AnimalRequest(farm, species, "GROWTH-PAGING", Sex.Female, ProductivePurpose.Meat));
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var index = 0; index < 21; index++)
            await Create(client, "weights", new WeightRequest(farm, animal, date.AddDays(-index), 100 + index));
        var first = await client.GetFromJsonAsync<AnimalGrowthResult>($"/api/animals/{animal}/growth?page=1&pageSize=10");
        var second = await client.GetFromJsonAsync<AnimalGrowthResult>($"/api/animals/{animal}/growth?page=2&pageSize=10");
        Assert.Equal(21, second!.Total);
        Assert.Equal(10, second.Records.Count);
        Assert.Equal(2, second.Page);
        Assert.Equal(first!.Points, second.Points);
        Assert.DoesNotContain(second.Records, record => first.Records.Any(previous => previous.Id == record.Id));
        using var invalid = await client.GetAsync($"/api/animals/{animal}/growth?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task EditingBirthDateCannotMoveItAfterHistoricalWeights()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var body = new AnimalRequest(farm, species, "BIRTH-WEIGHT", Sex.Female, ProductivePurpose.Meat, BirthDate: new DateOnly(2020, 1, 1));
        var animal = await Create(client, "animals", body);
        await Create(client, "weights", new WeightRequest(farm, animal, new DateOnly(2026, 1, 10), 100));
        using var invalid = await client.PutAsJsonAsync($"/api/animals/{animal}", body with { BirthDate = new DateOnly(2026, 1, 11) });
        Assert.Equal(HttpStatusCode.Conflict, invalid.StatusCode);
        var preserved = await client.GetFromJsonAsync<AnimalDetail>($"/api/animals/{animal}");
        Assert.Equal(body.BirthDate, preserved!.BirthDate);
        using var correction = await client.PutAsJsonAsync($"/api/animals/{animal}", body with { BirthDate = new DateOnly(2020, 1, 2) });
        Assert.Equal(HttpStatusCode.OK, correction.StatusCode);
    }

    [Fact]
    public async Task AnimalWeighingEndpointReplaysWithoutDuplicateRecordsAndRejectsChangedPayload()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "employee");
        var (farm, species) = await DemoIds(factory);
        var animal = await Create(client, "animals", new AnimalRequest(farm, species, "WEIGH-REPLAY", Sex.Male, ProductivePurpose.Meat));
        var request = new AnimalWeighingRequest(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), 125.5m, 3, " Field check ");
        using var first = await client.PostAsJsonAsync($"/api/animals/{animal}/weights", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        using var replay = await client.PostAsJsonAsync($"/api/animals/{animal}/weights", request);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.True((await replay.Content.ReadFromJsonAsync<AnimalWeighingResult>())!.Replayed);
        using var collision = await client.PostAsJsonAsync($"/api/animals/{animal}/weights", request with { WeightKg = 200 });
        Assert.Equal(HttpStatusCode.Conflict, collision.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var record = await db.WeightRecords.SingleAsync(r => r.AnimalId == animal);
        Assert.Equal(request.SubmissionId, record.Id);
        Assert.Equal(125.5m, record.WeightKg);
        Assert.Equal("Field check", record.Notes);
        Assert.NotNull(record.RecordedByUserId);
        Assert.Single(await db.AuditLogs.Where(r => r.EntityId == record.Id.ToString()).ToListAsync());
    }

    [Fact]
    public async Task GrowthAndCurrentWeightAgreeForMultipleWeighingsOnOneDate()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var animal = await Create(client, "animals", new AnimalRequest(farm, species, "GROWTH-DAILY", Sex.Female, ProductivePurpose.Meat));
        var day = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10);
        foreach (var request in new[] { new WeightRequest(farm, animal, day, 100), new WeightRequest(farm, animal, day.AddDays(10), 110), new WeightRequest(farm, animal, day.AddDays(10), 112) })
            await Create(client, "weights", request);
        var growth = await client.GetFromJsonAsync<AnimalGrowthResult>($"/api/animals/{animal}/growth");
        Assert.Equal(3, growth!.Records.Count);
        Assert.Equal(2, growth.Points.Count);
        Assert.Equal(1.2m, growth.Points.Last().DailyGainKg);
        var detail = await client.GetFromJsonAsync<AnimalDetail>($"/api/animals/{animal}");
        Assert.Equal(growth.Points.Last().WeightKg, detail!.CurrentWeightKg);
    }

    [Fact]
    public async Task UnassignedFarmIsHiddenForGrowthAndWeighing()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        var (_, species) = await DemoIds(factory);
        var farm = await Create(client, "farms", new FarmRequest("Private farm", "PRIVATE-GROWTH"));
        var animal = await Create(client, "animals", new AnimalRequest(farm, species, "PRIVATE-WEIGH", Sex.Male, ProductivePurpose.Meat));
        await Login(client, "employee");
        using var growth = await client.GetAsync($"/api/animals/{animal}/growth");
        Assert.Equal(HttpStatusCode.NotFound, growth.StatusCode);
        using var weight = await client.PostAsJsonAsync($"/api/animals/{animal}/weights", new AnimalWeighingRequest(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), 100));
        Assert.Equal(HttpStatusCode.NotFound, weight.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().WeightRecords.AnyAsync(r => r.AnimalId == animal));
    }

    [Fact]
    public async Task InvalidWeighingReturnsFieldErrorsAndInactiveAnimalIsRejected()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var body = new AnimalRequest(farm, species, "INACTIVE-WEIGH", Sex.Female, ProductivePurpose.Meat);
        var animal = await Create(client, "animals", body);
        using var invalid = await client.PostAsJsonAsync($"/api/animals/{animal}/weights", new AnimalWeighingRequest(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), -1));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains("WeightKg", await invalid.Content.ReadAsStringAsync());
        using var update = await client.PutAsJsonAsync($"/api/animals/{animal}", body with { Status = AnimalStatus.Sold });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        using var inactive = await client.PostAsJsonAsync($"/api/animals/{animal}/weights", new AnimalWeighingRequest(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), 100));
        Assert.Equal(HttpStatusCode.Conflict, inactive.StatusCode);
    }

    [Fact]
    public async Task ParentSearchAppliesSpeciesSexExclusionAndFarmBeforeCount()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "employee");
        var (farm, species) = await DemoIds(factory);
        var first = await Create(client, "animals", new AnimalRequest(farm, species, "PICKER-1", Sex.Female, ProductivePurpose.Meat));
        await Create(client, "animals", new AnimalRequest(farm, species, "PICKER-2", Sex.Female, ProductivePurpose.Meat));
        await Create(client, "animals", new AnimalRequest(farm, species, "PICKER-3", Sex.Male, ProductivePurpose.Meat));
        var result = await client.GetFromJsonAsync<AnimalPageResult>($"/api/animals/page?farmId={farm}&speciesId={species}&sex=Female&excludeId={first}&search=PICKER");
        Assert.Equal(1, result!.Total);
        Assert.Equal("PICKER-2", Assert.Single(result.Items).InternalTag);
    }
}
