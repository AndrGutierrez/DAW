using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Application.Livestock;
using Core.Application.Management;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Tests;

public sealed partial class ManagementIntegrationTests
{
    private static readonly JsonSerializerOptions CareJson = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private static async Task<Guid> CareProduct(HttpClient client, int? withdrawal = 5)
    {
        var category = await Create(client, "categories", new CategoryRequest("Clinical test " + Guid.NewGuid()));
        return await Create(client, "products", new ProductRequest("CARE-" + Guid.NewGuid().ToString("N")[..12], "Clinical test product", category, 10, 5, MeasurementUnit.Dose, WithdrawalDays: withdrawal));
    }
    [Fact]
    public async Task EmployeeClinicalSubmissionReplaysAndHistoryKeepsSnapshot()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var product = await CareProduct(client);
        var (farm, species) = await DemoIds(factory);
        await Login(client, "employee");
        var animal = await Create(client, "animals", new AnimalRequest(farm, species, "CLINICAL-REPLAY", Sex.Female, ProductivePurpose.Milk));
        var q = new ClinicalRequest(Guid.NewGuid(), ClinicalEventKind.Treatment, new(2026, 1, 1), " Preserve ", product, 2, MedicationRoute.Oral, new(2026, 1, 3));
        using var first = await client.PostAsJsonAsync($"/api/animals/{animal}/clinical", q);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        using var replay = await client.PostAsJsonAsync($"/api/animals/{animal}/clinical", q);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var collision = await client.PostAsJsonAsync($"/api/animals/{animal}/clinical", q with { Dose = 3 });
        Assert.Equal(HttpStatusCode.Conflict, collision.StatusCode);
        var history = await client.GetFromJsonAsync<ClinicalHistory>($"/api/animals/{animal}/clinical", CareJson);
        var record = Assert.Single(history!.History.Items);
        Assert.Equal(new DateOnly(2026, 1, 8), record.WithdrawalEndDate);
        Assert.NotNull(record.UserId); Assert.Contains("Clinical test product", record.ProductName);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Single(await db.HealthEvents.Where(x => x.AnimalId == animal).ToListAsync());
        Assert.Single(await db.AuditLogs.Where(x => x.EntityId == q.SubmissionId.ToString()).ToListAsync());
    }
    [Theory]
    [InlineData(ProductionMethod.Milking, AnimalProductType.Milk, MeasurementUnit.Liter)]
    [InlineData(ProductionMethod.Slaughter, AnimalProductType.Meat, MeasurementUnit.Kilogram)]
    public async Task WithdrawalBlocksBothProductionRoutesAndReleasesFollowingDay(ProductionMethod method, AnimalProductType productType, MeasurementUnit unit)
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var product = await CareProduct(client); var (farm, species) = await DemoIds(factory);
        var animal = await Create(client, "animals", new AnimalRequest(farm, species, "RESTRICT-" + method, Sex.Female, ProductivePurpose.DualPurpose));
        using var clinical = await client.PostAsJsonAsync($"/api/animals/{animal}/clinical", new ClinicalRequest(Guid.NewGuid(), ClinicalEventKind.Treatment, new(2026, 1, 1), ProductId: product, Dose: 1, EndDate: new(2026, 1, 3)));
        Assert.Equal(HttpStatusCode.Created, clinical.StatusCode);
        var body = new ProductionRequest(farm, animal, new(2026, 1, 8), productType, method, 10, unit, Guid.NewGuid());
        using var legacy = await client.PostAsJsonAsync("/api/production", body);
        Assert.Equal(HttpStatusCode.Conflict, legacy.StatusCode);
        using var nested = await client.PostAsJsonAsync($"/api/animals/{animal}/production", new AnimalProductionRequest(Guid.NewGuid(), body.Date, productType, method, 10, unit));
        Assert.Equal(HttpStatusCode.Conflict, nested.StatusCode);
        using var allowed = await client.PostAsJsonAsync("/api/production", body with { Date = new(2026, 1, 9) });
        Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
        if (method == ProductionMethod.Slaughter)
            Assert.Equal("Dead", (await client.GetFromJsonAsync<AnimalDetail>($"/api/animals/{animal}"))!.Status);
    }
    [Fact]
    public async Task BackdatedTreatmentCannotInvalidateRecordedMilk()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var product = await CareProduct(client); var (farm, species) = await DemoIds(factory);
        var animal = await Create(client, "animals", new AnimalRequest(farm, species, "CARE-RETRO", Sex.Female, ProductivePurpose.Milk));
        await Create(client, "production", new ProductionRequest(farm, animal, new(2026, 1, 4), AnimalProductType.Milk, ProductionMethod.Milking, 5, MeasurementUnit.Liter, Guid.NewGuid()));
        using var treatment = await client.PostAsJsonAsync($"/api/animals/{animal}/clinical",
            new ClinicalRequest(Guid.NewGuid(), ClinicalEventKind.Treatment, new(2026, 1, 1), ProductId: product, Dose: 1, EndDate: new(2026, 1, 1)));
        Assert.Equal(HttpStatusCode.Conflict, treatment.StatusCode);
        Assert.Equal(0, (await client.GetFromJsonAsync<ClinicalHistory>($"/api/animals/{animal}/clinical", CareJson))!.History.Total);
    }
    [Fact]
    public async Task ClinicalPagesValidateBoundsAndHideUnassignedFarm()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var animal = await Create(client, "animals", new AnimalRequest(farm, species, "CARE-PAGE", Sex.Female, ProductivePurpose.Milk));
        for (var i = 0; i < 12; i++)
        {
            using var created = await client.PostAsJsonAsync($"/api/animals/{animal}/clinical",
                new ClinicalRequest(Guid.NewGuid(), ClinicalEventKind.Quarantine, new DateOnly(2026, 1, 1).AddDays(i), Reason: "Clinical test"));
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        }
        var second = await client.GetFromJsonAsync<ClinicalHistory>($"/api/animals/{animal}/clinical?page=2", CareJson);
        Assert.Equal(12, second!.History.Total); Assert.Equal(2, second.History.Page); Assert.Equal(2, second.History.Items.Count);
        using var invalid = await client.GetAsync($"/api/animals/{animal}/clinical?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var privateFarm = await Create(client, "farms", new FarmRequest("Private clinical farm", "PRIV-CARE"));
        var privateAnimal = await Create(client, "animals", new AnimalRequest(privateFarm, species, "CARE-HIDDEN", Sex.Female, ProductivePurpose.Milk));
        await Login(client, "employee");
        foreach (var resource in new[] { "clinical", "reproduction", "production" })
        {
            using var hidden = await client.GetAsync($"/api/animals/{privateAnimal}/{resource}");
            Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        }
    }
    [Fact]
    public async Task ReproductionStatusFollowsEventDateAndCalvingWithoutInventingPregnancyFromMating()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "employee");
        var (farm, species) = await DemoIds(factory);
        var animal = await Create(client, "animals", new AnimalRequest(farm, species, "REPRO-STATE", Sex.Female, ProductivePurpose.Milk));
        async Task Record(ReproductiveRequest q)
        {
            using var response = await client.PostAsJsonAsync($"/api/animals/{animal}/reproduction", q);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
        await Record(new(Guid.NewGuid(), ReproductiveEventKind.Mating, new(2026, 1, 1)));
        Assert.Equal("Unknown", (await client.GetFromJsonAsync<ReproductiveHistory>($"/api/animals/{animal}/reproduction", CareJson))!.Status.State);
        await Record(new(Guid.NewGuid(), ReproductiveEventKind.PregnancyCheck, new(2026, 2, 1), Result: PregnancyResult.Positive, ExpectedCalvingDate: new(2026, 9, 1)));
        await Record(new(Guid.NewGuid(), ReproductiveEventKind.PregnancyCheck, new(2026, 1, 2), Result: PregnancyResult.Negative));
        Assert.Equal("Pregnant", (await client.GetFromJsonAsync<ReproductiveHistory>($"/api/animals/{animal}/reproduction", CareJson))!.Status.State);
        await Record(new(Guid.NewGuid(), ReproductiveEventKind.Calving, new(2026, 9, 1), OffspringCount: 1, StillbornCount: 0));
        Assert.Equal("Calved", (await client.GetFromJsonAsync<ReproductiveHistory>($"/api/animals/{animal}/reproduction", CareJson))!.Status.State);
    }
    [Fact]
    public async Task CareHistoryProtectsIdentityAndMaleReproductionIsRejected()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var body = new AnimalRequest(farm, species, "CARE-IDENTITY", Sex.Female, ProductivePurpose.Milk, BirthDate: new(2020, 1, 1));
        var animal = await Create(client, "animals", body);
        using var eventResponse = await client.PostAsJsonAsync($"/api/animals/{animal}/clinical",
            new ClinicalRequest(Guid.NewGuid(), ClinicalEventKind.DiseaseCase, new(2026, 1, 1), Severity: "Observed"));
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
        using var changed = await client.PutAsJsonAsync($"/api/animals/{animal}", body with { Sex = Sex.Male });
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        var male = await Create(client, "animals", new AnimalRequest(farm, species, "REPRO-MALE", Sex.Male, ProductivePurpose.Meat));
        using var reproduction = await client.PostAsJsonAsync($"/api/animals/{male}/reproduction",
            new ReproductiveRequest(Guid.NewGuid(), ReproductiveEventKind.Heat, new(2026, 1, 1)));
        Assert.Equal(HttpStatusCode.Conflict, reproduction.StatusCode);
    }
    [Fact]
    public async Task ProductionSubmissionReplaysOneYieldAndRejectsDifferentQuantity()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "employee");
        var (farm, species) = await DemoIds(factory);
        var animal = await Create(client, "animals", new AnimalRequest(farm, species, "PRODUCTION-REPLAY", Sex.Female, ProductivePurpose.Milk));
        var q = new AnimalProductionRequest(Guid.NewGuid(), new(2026, 1, 1), AnimalProductType.Milk, ProductionMethod.Milking, 10, MeasurementUnit.Liter);
        using var first = await client.PostAsJsonAsync($"/api/animals/{animal}/production", q);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        using var replay = await client.PostAsJsonAsync($"/api/animals/{animal}/production", q);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var collision = await client.PostAsJsonAsync($"/api/animals/{animal}/production", q with { Quantity = 11 });
        Assert.Equal(HttpStatusCode.Conflict, collision.StatusCode);
        var records = await client.GetFromJsonAsync<CarePage<ResourceResult<ProductionRequest>>>($"/api/animals/{animal}/production", CareJson);
        Assert.Single(records!.Items);
    }
}
