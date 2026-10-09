using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Application.Management;
using Core.Application.Livestock;
using Core.Application.Operations;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace Core.Tests;
public sealed partial class ManagementIntegrationTests
{
    [Fact]
    public async Task ArchiveIsScopedToAdministratorsRetainsRowsAndRestoresWithAudits()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/archive")).StatusCode);
        await Login(client, "employee"); Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/archive")).StatusCode);
        await Login(client, "admin"); var (farm, species) = await DemoIds(factory);
        var id = await Create(client, "animals", new AnimalRequest(farm, species, "ARCHIVE-UNIQUE", Sex.Male, ProductivePurpose.Meat));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/animals/" + id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/animals/" + id)).StatusCode);
        var page = await client.GetFromJsonAsync<CarePage<ArchiveEntry>>("/api/admin/archive?resource=animals&search=archive-unique&pageSize=1");
        Assert.Equal(1, page!.Total); var entry = Assert.Single(page.Items); Assert.Equal(id, entry.Id); Assert.NotNull(entry.DeletedByUserId);
        var detail = await client.GetFromJsonAsync<ArchiveDetail>("/api/admin/archive/animals/" + id); Assert.Contains("ARCHIVE-UNIQUE", detail!.Values);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/admin/archive/animals/" + id + "/restore", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/animals/" + id)).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var retained = await db.Animals.IgnoreQueryFilters().SingleAsync(a => a.Id == id); Assert.False(retained.IsDeleted); Assert.Null(retained.DeletedAt);
        Assert.Single(await db.AuditLogs.Where(a => a.EntityId == id.ToString() && a.Action == "Archived").ToListAsync());
        Assert.Single(await db.AuditLogs.Where(a => a.EntityId == id.ToString() && a.Action == "Restored").ToListAsync());
    }
    [Theory]
    [InlineData("page=0")][InlineData("pageSize=101")][InlineData("resource=users")]
    public async Task ArchiveRejectsInvalidQueries(string query)
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/admin/archive?" + query)).StatusCode);
    }
    [Fact]
    public async Task RestoreRequiresLiveDependenciesAndSeedingDoesNotResurrectArchivedData()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        var category = await Create(client, "categories", new CategoryRequest("Archived dependency"));
        var product = await Create(client, "products", new ProductRequest("ARC-DEP", "Archived product", category, 1, 1, MeasurementUnit.Unit));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/products/" + product)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/categories/" + category)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/admin/archive/products/" + product + "/restore", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/admin/archive/categories/" + category + "/restore", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/admin/archive/products/" + product + "/restore", null)).StatusCode);
        var (farm, species) = await DemoIds(factory); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var demo = await db.Animals.FirstAsync(a => a.FarmId == farm);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/animals/" + demo.Id)).StatusCode);
        await Seed(factory); db.ChangeTracker.Clear();
        Assert.True((await db.Animals.IgnoreQueryFilters().SingleAsync(a => a.Id == demo.Id)).IsDeleted);
    }
    [Fact]
    public async Task ArchivePreservesAncestorIdentityAndPreventsSelectingArchivedParentsForNewOffspring()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin"); var (farm, species) = await DemoIds(factory);
        var mother = await Create(client, "animals", new AnimalRequest(farm, species, "ARC-MOTHER", Sex.Female, ProductivePurpose.Milk, BirthDate: new(2020,1,1)));
        var request = new AnimalRequest(farm, species, "ARC-CHILD", Sex.Female, ProductivePurpose.Milk, BirthDate: new(2024,1,1), DamId: mother);
        var child = await Create(client, "animals", request);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/animals/" + mother)).StatusCode);
        var lineage = await client.GetFromJsonAsync<AnimalLineage>("/api/animals/" + mother + "/lineage"); Assert.True(lineage!.IsArchived); Assert.Equal("ARC-MOTHER", lineage.InternalTag);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/animals/" + child, request with { Name = "Updated child" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/animals", request with { InternalTag = "ARC-NEW-CHILD" })).StatusCode);
    }
    [Fact]
    public async Task AnnulledSlaughterYieldCanBeRestoredWithoutReactivatingTheAnimal()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin"); var (farm, species) = await DemoIds(factory);
        var animalRequest = new AnimalRequest(farm, species, "ARC-SLAUGHTER", Sex.Male, ProductivePurpose.Meat);
        var animal = await Create(client, "animals", animalRequest); var day = DateOnly.FromDateTime(DateTime.UtcNow);
        var production = await Create(client, "production", new ProductionRequest(farm, animal, day, AnimalProductType.Meat, ProductionMethod.Slaughter, 250, MeasurementUnit.Kilogram, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/production/" + production)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/animals/" + animal, animalRequest)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/weights", new WeightRequest(farm, animal, day, 400))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/admin/archive/production/" + production + "/restore", null)).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); Assert.Equal(AnimalStatus.Dead, (await db.Animals.SingleAsync(a => a.Id == animal)).Status);
    }
    [Fact]
    public async Task DirectEfRemovalRetainsTheRowAndAuditHistoryRejectsDeletion()
    {
        using var factory = Factory(); await Seed(factory); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var animal = await db.Animals.FirstAsync(); db.Animals.Remove(animal); await db.SaveChangesAsync(acceptAllChangesOnSuccess: true);
        Assert.False(await db.Animals.AnyAsync(a => a.Id == animal.Id)); Assert.True(await db.Animals.IgnoreQueryFilters().AnyAsync(a => a.Id == animal.Id && a.IsDeleted));
        var audit = new AuditLog { EntityName = "Animal", Action = "Added", EntityId = animal.Id.ToString() }; db.AuditLogs.Add(audit); await db.SaveChangesAsync(); db.AuditLogs.Remove(audit);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}
