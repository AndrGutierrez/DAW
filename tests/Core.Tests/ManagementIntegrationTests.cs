using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Tests;

public sealed partial class ManagementIntegrationTests
{
    [Fact]
    public async Task EveryManagedResourceSupportsCreateReadUpdateDelete()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        var farm = await Create(client, "farms", new { name = "CRUD Farm", code = "CRUD" });
        var species = await Create(client, "species", new { name = "CRUD Sheep", code = "OV", purpose = "Wool" }, "TEST-SPECIES");
        var breed = await Create(client, "breeds", new { speciesId = species, name = "CRUD Breed", purpose = "Wool" });
        var paddock = await Create(client, "paddocks", new { farmId = farm, name = "CRUD Paddock" });
        var lot = await Create(client, "lots", new { farmId = farm, speciesId = species, name = "CRUD Lot", purpose = "Wool", paddockId = paddock });
        var category = await Create(client, "categories", new { name = "CRUD Category" });
        var product = await Create(client, "products", new { sku = "CRUD-001", name = "Feed", categoryId = category, price = 20.25, costPrice = 10.50, unit = "Bag" });
        var inventory = await Create(client, "inventory", new { farmId = farm, productId = product, stock = 20, minStock = 2, maxStock = 100, location = "Warehouse" });
        var animal = await Create(client, "animals", new { farmId = farm, speciesId = species, breedId = breed, lotId = lot, paddockId = paddock, internalTag = "crud-001", sex = "Female", purpose = "Wool" });
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var weight = await Create(client, "weights", new { farmId = farm, animalId = animal, date, weightKg = 45.25 });
        var production = await Create(client, "production", new { farmId = farm, animalId = animal, date, productType = "Other", method = "Collection", quantity = 1, unit = "Unit", operationId = Guid.NewGuid() });
        var items = new[]
        {
            ("production", production),
            ("weights", weight),
            ("animals", animal),
            ("inventory", inventory),
            ("products", product),
            ("categories", category),
            ("lots", lot),
            ("paddocks", paddock),
            ("breeds", breed),
            ("species", species),
            ("farms", farm)
        };
        foreach (var (route, id) in items)
        {
            using var get = await client.GetAsync($"/api/{route}/{id}");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            using var document = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
            object body;
            if (route == "animals")
                body = new
                {
                    farmId = farm,
                    speciesId = species,
                    breedId = breed,
                    lotId = lot,
                    paddockId = paddock,
                    internalTag = "CRUD-001",
                    sex = "Female",
                    purpose = "Wool",
                    name = "Updated"
                };
            else
                body = document.RootElement.GetProperty("data").Clone();
            using var update = await client.PutAsJsonAsync($"/api/{route}/{id}", body);
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            using var delete = await client.DeleteAsync($"/api/{route}/{id}");
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            using var missing = await client.GetAsync($"/api/{route}/{id}");
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }
    }

    [Fact]
    public async Task RestoredRegistrationNormalizesTagsAndHealthRouteWritesHistory()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var request = new AnimalRequest(farm, species, " duplicate-1 ", Sex.Female, ProductivePurpose.Milk);
        var id = await Create(client, "animals", request);
        using var duplicate = await client.PostAsJsonAsync("/api/animals", request with { InternalTag = "DUPLICATE-1" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var health = await client.PatchAsJsonAsync($"/api/animals/{id}/health-status", new { status = "InTreatment", reason = "Veterinary check" });
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Animals.SingleAsync(x => x.Id == id);
        Assert.Equal("DUPLICATE-1", saved.InternalTag);
        Assert.Equal(HealthStatus.InTreatment, saved.HealthStatus);
        var change = await db.HealthStatusChanges.SingleAsync(x => x.AnimalId == id);
        Assert.Equal(HealthStatus.Healthy, change.PreviousStatus);
        Assert.Equal("Veterinary check", change.Reason);
        Assert.True(await db.AuditLogs.AnyAsync(x => x.EntityId == id.ToString()));
    }

    [Fact]
    public async Task EmployeeCanOperateAnimalsAndInventoryButCannotMaintainCatalogs()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "employee");
        var (farm, species) = await DemoIds(factory);
        var id = await Create(client, "animals", new AnimalRequest(farm, species, "EMP-1", Sex.Male, ProductivePurpose.Meat));
        using var delete = await client.DeleteAsync($"/api/animals/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal("application/problem+json", delete.Content.Headers.ContentType?.MediaType);
        using var category = await client.PostAsJsonAsync("/api/categories", new { name = "Unauthorized Category" });
        Assert.Equal(HttpStatusCode.Forbidden, category.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = await db.Products.AsNoTracking().OrderBy(x => x.SKU).FirstAsync();
        using var productRead = await client.GetAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.OK, productRead.StatusCode);
        using var productCreate = await client.PostAsJsonAsync("/api/products", new ProductRequest("EMP-NEW-001", "Unauthorized product", product.CategoryId, 10, 5, MeasurementUnit.Unit));
        Assert.Equal(HttpStatusCode.Forbidden, productCreate.StatusCode);
        using var productUpdate = await client.PutAsJsonAsync($"/api/products/{product.Id}", new ProductRequest(product.SKU, "Unauthorized change", product.CategoryId, 999, 998, product.Unit, product.Brand));
        Assert.Equal(HttpStatusCode.Forbidden, productUpdate.StatusCode);
        using var productDelete = await client.DeleteAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, productDelete.StatusCode);
        Assert.Equal("application/problem+json", productDelete.Content.Headers.ContentType?.MediaType);
        var preserved = await db.Products.AsNoTracking().SingleAsync(x => x.Id == product.Id);
        Assert.Equal(product.Name, preserved.Name);
        Assert.Equal(product.Price, preserved.Price);
        Assert.Equal(product.CostPrice, preserved.CostPrice);
        Assert.False(await db.Products.AnyAsync(x => x.SKU == "EMP-NEW-001"));

        var inventory = await db.FarmInventory.AsNoTracking().SingleAsync(x => x.FarmId == farm && x.ProductId == product.Id);
        var adjustment = new InventoryRequest(farm, product.Id, inventory.Stock + 1, inventory.MinStock, inventory.MaxStock, inventory.Location);
        using var inventoryUpdate = await client.PutAsJsonAsync($"/api/inventory/{inventory.Id}", adjustment);
        Assert.Equal(HttpStatusCode.OK, inventoryUpdate.StatusCode);
        Assert.Equal(adjustment.Stock, (await db.FarmInventory.AsNoTracking().SingleAsync(x => x.Id == inventory.Id)).Stock);
    }

    [Fact]
    public async Task AdministratorReceivesFieldErrorsForNegativeProductPrice()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoryId = await db.InventoryCategories.Select(x => x.Id).FirstAsync();
        using var invalid = await client.PostAsJsonAsync("/api/products", new { sku = "BAD-1", name = "Invalid", categoryId, price = -10, costPrice = 1, unit = "Unit" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var problem = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("Price", out _));
        Assert.False(await db.Products.AnyAsync(x => x.SKU == "BAD-1"));
    }

    [Theory]
    [InlineData("BAD SKU")]
    [InlineData("BAD_SKU")]
    [InlineData("BAD/001")]
    public async Task AdministratorCannotCreateAProductWithAnInvalidSku(string sku)
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoryId = await db.InventoryCategories.Select(x => x.Id).FirstAsync();
        using var response = await client.PostAsJsonAsync("/api/products", new ProductRequest(sku, "Invalid SKU", categoryId, 10, 5, MeasurementUnit.Unit));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("SKU", out _));
        Assert.False(await db.Products.AnyAsync(x => x.SKU == sku));
    }

    [Fact]
    public async Task ProductDtoDoesNotBindIdentityOrCreationTimestamp()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoryId = await db.InventoryCategories.Select(x => x.Id).FirstAsync();
        var injectedId = Guid.NewGuid();
        var injectedCreatedAt = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        using var create = await client.PostAsJsonAsync("/api/products", new
        {
            id = injectedId,
            createdAt = injectedCreatedAt,
            sku = "DTO-IDENTITY-001",
            name = "Input fields only",
            categoryId,
            price = 10,
            costPrice = 5,
            unit = "Unit"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();
        var createdAt = created.RootElement.GetProperty("createdAt").GetDateTime();
        Assert.NotEqual(injectedId, id);
        Assert.NotEqual(injectedCreatedAt, createdAt);

        using var update = await client.PutAsJsonAsync($"/api/products/{id}", new
        {
            id = injectedId,
            createdAt = injectedCreatedAt,
            sku = "DTO-IDENTITY-001",
            name = "Authorized name update",
            categoryId,
            price = 12,
            costPrice = 6,
            unit = "Unit"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        using var get = await client.GetAsync($"/api/products/{id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        using var persisted = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.Equal(id, persisted.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(createdAt, persisted.RootElement.GetProperty("createdAt").GetDateTime());
        Assert.Equal("Authorized name update", persisted.RootElement.GetProperty("data").GetProperty("name").GetString());
        var stored = await db.Products.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(createdAt, stored.CreatedAt);
        Assert.Equal("Authorized name update", stored.Name);
        Assert.False(await db.Products.AnyAsync(x => x.Id == injectedId));
    }

    [Fact]
    public async Task AnimalCannotUseAnotherSpeciesBreedOrAnotherFarmsLot()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var wrongBreed = await db.Breeds.FirstAsync(x => x.SpeciesId != species);
        using var response = await client.PostAsJsonAsync("/api/animals", new AnimalRequest(farm, species, "WRONG-1", Sex.Female, ProductivePurpose.Milk, BreedId: wrongBreed.Id));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.False(await db.Animals.AnyAsync(x => x.InternalTag == "WRONG-1"));
    }

    [Fact]
    public async Task OneAnimalProducesMultipleProductsAndSlaughterDisallowsLaterMilking()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var id = await Create(client, "animals", new AnimalRequest(farm, species, "PROD-1", Sex.Female, ProductivePurpose.DualPurpose));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await Create(client, "production", new ProductionRequest(farm, id, today.AddDays(-1), AnimalProductType.Milk, ProductionMethod.Milking, 10, MeasurementUnit.Liter, Guid.NewGuid()));
        var operation = Guid.NewGuid();
        await Create(client, "production", new ProductionRequest(farm, id, today, AnimalProductType.Meat, ProductionMethod.Slaughter, 250, MeasurementUnit.Kilogram, operation));
        await Create(client, "production", new ProductionRequest(farm, id, today, AnimalProductType.Hide, ProductionMethod.Slaughter, 1, MeasurementUnit.Unit, operation));
        using var invalid = await client.PostAsJsonAsync("/api/production", new ProductionRequest(farm, id, today, AnimalProductType.Milk, ProductionMethod.Milking, 10, MeasurementUnit.Liter, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Conflict, invalid.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(3, await db.AnimalProduction.CountAsync(x => x.AnimalId == id));
        Assert.Equal(AnimalStatus.Dead, (await db.Animals.SingleAsync(x => x.Id == id)).Status);
    }

    [Fact]
    public async Task BackdatedSlaughterCannotInvalidateAnExistingLiveWeight()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var animal = await Create(client, "animals", new AnimalRequest(farm, species, "WEIGHT-CHRONOLOGY", Sex.Male, ProductivePurpose.Meat));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await Create(client, "weights", new WeightRequest(farm, animal, today, 450));

        using var response = await client.PostAsJsonAsync("/api/production", new ProductionRequest(farm, animal, today.AddDays(-1), AnimalProductType.Meat, ProductionMethod.Slaughter, 250, MeasurementUnit.Kilogram, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(AnimalStatus.Active, (await db.Animals.SingleAsync(x => x.Id == animal)).Status);
        Assert.False(await db.AnimalProduction.AnyAsync(x => x.AnimalId == animal));
    }

    [Fact]
    public async Task WeightRecordedBeforeSameDaySlaughterCanBeCorrectedButNewWeightIsRejected()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var animal = await Create(client, "animals", new AnimalRequest(farm, species, "SAME-DAY-WEIGHT", Sex.Male, ProductivePurpose.Meat));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new WeightRequest(farm, animal, today, 450);
        var weight = await Create(client, "weights", request);
        await Create(client, "production", new ProductionRequest(farm, animal, today, AnimalProductType.Meat, ProductionMethod.Slaughter, 250, MeasurementUnit.Kilogram, Guid.NewGuid()));

        using var correction = await client.PutAsJsonAsync($"/api/weights/{weight}", request with { WeightKg = 455 });
        Assert.Equal(HttpStatusCode.OK, correction.StatusCode);
        using var newWeight = await client.PostAsJsonAsync("/api/weights", request);
        Assert.Equal(HttpStatusCode.Conflict, newWeight.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(455m, (await db.WeightRecords.SingleAsync(x => x.AnimalId == animal)).WeightKg);
    }

    [Fact]
    public async Task RecordedParentIdentityCannotBeChangedWhileOffspringReferenceIt()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var parentRequest = new AnimalRequest(farm, species, "PARENT-MOTHER", Sex.Female, ProductivePurpose.Milk, BirthDate: today.AddYears(-4));
        var mother = await Create(client, "animals", parentRequest);
        await Create(client, "animals", new AnimalRequest(farm, species, "PARENT-CHILD", Sex.Female, ProductivePurpose.Milk, BirthDate: today.AddYears(-1), DamId: mother));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var otherSpecies = await db.Species.Where(x => x.Id != species).Select(x => x.Id).FirstAsync();
        var mutations = new[]
        {
            parentRequest with { Sex = Sex.Male },
            parentRequest with { SpeciesId = otherSpecies },
            parentRequest with { BirthDate = today.AddYears(-3) }
        };
        foreach (var request in mutations)
        {
            using var response = await client.PutAsJsonAsync($"/api/animals/{mother}", request);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
        var saved = await db.Animals.SingleAsync(x => x.Id == mother);
        Assert.Equal(Sex.Female, saved.Sex);
        Assert.Equal(species, saved.SpeciesId);
        Assert.Equal(parentRequest.BirthDate, saved.BirthDate);
    }

    [Fact]
    public async Task AnimalPutCannotBypassHealthRestrictionAfterSlaughter()
    {
        using var factory = Factory();
        await Seed(factory);
        using var client = factory.CreateClient();
        await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var request = new AnimalRequest(farm, species, "DEAD-HEALTH", Sex.Male, ProductivePurpose.Meat);
        var animal = await Create(client, "animals", request);
        await Create(client, "production", new ProductionRequest(farm, animal, DateOnly.FromDateTime(DateTime.UtcNow), AnimalProductType.Meat, ProductionMethod.Slaughter, 250, MeasurementUnit.Kilogram, Guid.NewGuid()));

        using var put = await client.PutAsJsonAsync($"/api/animals/{animal}", request with { Status = AnimalStatus.Dead, HealthStatus = HealthStatus.InTreatment });
        Assert.Equal(HttpStatusCode.Conflict, put.StatusCode);
        using var patch = await client.PatchAsJsonAsync($"/api/animals/{animal}/health-status", new { status = "InTreatment" });
        Assert.Equal(HttpStatusCode.Conflict, patch.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Animals.SingleAsync(x => x.Id == animal);
        Assert.Equal(AnimalStatus.Dead, saved.Status);
        Assert.Equal(HealthStatus.Healthy, saved.HealthStatus);
        Assert.False(await db.HealthStatusChanges.AnyAsync(x => x.AnimalId == animal));
    }

    [Fact]
    public async Task SeedingIsIdempotentAndReadsDoNotTrackEntities()
    {
        using var factory = Factory();
        await Seed(factory);
        await Seed(factory);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(4, await db.Products.CountAsync());
        Assert.Equal(4, await db.FarmInventory.CountAsync());
        Assert.Equal(4, await db.AnimalProduction.CountAsync());
        db.ChangeTracker.Clear();
        var repository = scope.ServiceProvider.GetRequiredService<IManagementRepository>();
        await repository.ListAsync<Product>();
        Assert.Empty(db.ChangeTracker.Entries<Product>());
    }

    private static async Task<Guid> Create(HttpClient client, string route, object body, string? codeOverride = null)
    {
        if (codeOverride != null)
            body = new
            {
                name = "CRUD Species",
                code = codeOverride,
                purpose = "Wool"
            };
        using var response = await client.PostAsJsonAsync($"/api/{route}", body);
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"{route}: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task Login(HttpClient client, string username)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password = "Test123!Pass" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", doc.RootElement.GetProperty("accessToken").GetString());
    }

    private static async Task Seed(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
    }

    private static async Task<(Guid Farm, Guid Species)> DemoIds(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.Farms.Where(x => x.Code == "DEMO").Select(x => x.Id).SingleAsync(), await db.Species.Where(x => x.Code == "BO").Select(x => x.Id).SingleAsync());
    }

    private static WebApplicationFactory<Program> Factory()
    {
        var name = Guid.NewGuid().ToString();
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["DisableHttpsRedirection"] = "true", ["Jwt:Key"] = new string('t', 64), ["Seed:AdminPassword"] = "Test123!Pass", ["Seed:EmployeePassword"] = "Test123!Pass" }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(name));
            });
        });
    }
}
