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
    public async Task BothAnimalWriteRoutesEnforceCapacityAndReactivation()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var p = await Create(client, "paddocks", new PaddockRequest(farm, "Limited", AreaHectares: 2, Capacity: 1));
        var a = new AnimalRequest(farm, species, "CAP-ONE", Sex.Female, ProductivePurpose.Milk, PaddockId: p);
        var first = await Create(client, "animals", a);
        using var excess = await client.PostAsJsonAsync("/api/animals", a with { InternalTag = "CAP-TWO" });
        Assert.Equal(HttpStatusCode.Conflict, excess.StatusCode);
        using var unchanged = await client.PutAsJsonAsync($"/api/animals/{first}", a with { Name = "Renamed" });
        Assert.Equal(HttpStatusCode.OK, unchanged.StatusCode);
        var inactive = await Create(client, "animals", a with { InternalTag = "CAP-SOLD", Status = AnimalStatus.Sold });
        using var reactivate = await client.PutAsJsonAsync($"/api/animals/{inactive}", a with { InternalTag = "CAP-SOLD" });
        Assert.Equal(HttpStatusCode.Conflict, reactivate.StatusCode);
        using var deactivate = await client.PutAsJsonAsync($"/api/paddocks/{p}", new PaddockRequest(farm, "Limited", Capacity: 1, IsActive: false));
        Assert.Equal(HttpStatusCode.Conflict, deactivate.StatusCode);
    }
    [Fact]
    public async Task MovementReplaysPreservesMetadataAndMakesArrivalKnown()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var p = await Create(client, "paddocks", new PaddockRequest(farm, "Arrival", AreaHectares: 2, Capacity: 3));
        var animal = await Create(client, "animals", new AnimalRequest(farm, species, "MOVE-ONE", Sex.Female, ProductivePurpose.Milk, Name: "Keep me"));
        var q = new AnimalMovementRequest(Guid.NewGuid(), p, null, null, null, " Rotation ");
        using var moved = await client.PostAsJsonAsync($"/api/animals/{animal}/movements", q);
        Assert.Equal(HttpStatusCode.Created, moved.StatusCode);
        using var replay = await client.PostAsJsonAsync($"/api/animals/{animal}/movements", q);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var collision = await client.PostAsJsonAsync($"/api/animals/{animal}/movements", q with { Reason = "Changed" });
        Assert.Equal(HttpStatusCode.Conflict, collision.StatusCode);
        var detail = await client.GetFromJsonAsync<AnimalDetail>($"/api/animals/{animal}");
        Assert.Equal("Keep me", detail!.Name); Assert.Equal(p, detail.PaddockId);
        var residents = await client.GetFromJsonAsync<CarePage<PaddockResident>>($"/api/paddocks/{p}/residents");
        var resident = Assert.Single(residents!.Items);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), resident.ArrivalDate);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Single(await db.AnimalMovements.Where(m => m.AnimalId == animal).ToListAsync());
    }
    [Fact]
    public async Task PageFiltersBeforePaginationAndReturnsRealOccupancy()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        for (var i = 0; i < 3; i++)
        {
            var p = await Create(client, "paddocks", new PaddockRequest(farm, "MAP-" + i, AreaHectares: 2, Capacity: 5));
            await Create(client, "animals", new AnimalRequest(farm, species, "MAP-A-" + i, Sex.Female, ProductivePurpose.Milk, PaddockId: p));
            await Create(client, "animals", new AnimalRequest(farm, species, "MAP-S-" + i, Sex.Female, ProductivePurpose.Milk, PaddockId: p, Status: AnimalStatus.Sold));
        }
        var page = await client.GetFromJsonAsync<CarePage<PaddockSnapshot>>($"/api/paddocks/page?search=MAP-&farmId={farm}&page=2&pageSize=2");
        Assert.Equal(3, page!.Total); Assert.Equal(2, page.Page);
        var item = Assert.Single(page.Items); Assert.Equal(1, item.Occupancy); Assert.Equal(0, item.UnknownArrivals);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), item.OldestKnownArrival);
    }
    [Fact]
    public async Task LegacyAnimalHasUnknownArrivalAndLotReferenceDoesNotCountAsLocation()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var p = await Create(client, "paddocks", new PaddockRequest(farm, "Legacy-map", Capacity: 4));
        var lot = await Create(client, "lots", new LotRequest(farm, species, "Reference only", ProductivePurpose.Milk, p));
        await Create(client, "animals", new AnimalRequest(farm, species, "LOT-ONLY", Sex.Female, ProductivePurpose.Milk, LotId: lot));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Animals.Add(new Animal { FarmId = farm, SpeciesId = species, InternalTag = "LEGACY-LOCATION", PaddockId = p });
            await db.SaveChangesAsync();
        }
        var page = await client.GetFromJsonAsync<CarePage<PaddockSnapshot>>("/api/paddocks/page?search=Legacy-map");
        var item = Assert.Single(page!.Items);
        Assert.Equal(1, item.Occupancy); Assert.Equal(1, item.UnknownArrivals); Assert.Null(item.OldestKnownArrival);
    }
    [Fact]
    public async Task StaleSourceAndFullDestinationDoNotWriteMovement()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var p = await Create(client, "paddocks", new PaddockRequest(farm, "Full", Capacity: 1));
        await Create(client, "animals", new AnimalRequest(farm, species, "RESIDENT", Sex.Female, ProductivePurpose.Milk, PaddockId: p));
        var animal = await Create(client, "animals", new AnimalRequest(farm, species, "WAITING", Sex.Female, ProductivePurpose.Milk));
        var q = new AnimalMovementRequest(Guid.NewGuid(), p, null, null, null, "Rotation");
        using var full = await client.PostAsJsonAsync($"/api/animals/{animal}/movements", q);
        Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);
        using var stale = await client.PostAsJsonAsync($"/api/animals/{animal}/movements", q with { ExpectedFromPaddockId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var history = await client.GetFromJsonAsync<CarePage<AnimalMovementRecord>>($"/api/animals/{animal}/movements");
        Assert.Empty(history!.Items);
    }
    [Fact]
    public async Task ReducingCapacityBelowOccupancyIsRejected()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var p = await Create(client, "paddocks", new PaddockRequest(farm, "Resize", Capacity: 3));
        for (var i = 0; i < 2; i++) await Create(client, "animals", new AnimalRequest(farm, species, "RESIZE-" + i, Sex.Female, ProductivePurpose.Milk, PaddockId: p));
        using var update = await client.PutAsJsonAsync($"/api/paddocks/{p}", new PaddockRequest(farm, "Resize", Capacity: 1));
        Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
    }

    [Fact]
    public async Task LotOnlyUpdateDoesNotResetPhysicalArrivalDate()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var p = await Create(client, "paddocks", new PaddockRequest(farm, "Stable arrival", Capacity: 4));
        var lot = await Create(client, "lots", new LotRequest(farm, species, "New group", ProductivePurpose.Milk));
        var q = new AnimalRequest(farm, species, "STABLE-ENTRY", Sex.Female, ProductivePurpose.Milk);
        var animal = await Create(client, "animals", q);
        var entered = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Animals.SingleAsync(a => a.Id == animal)).PaddockId = p;
            db.AnimalMovements.Add(new AnimalMovement { AnimalId = animal, FarmId = farm, ToPaddockId = p, Date = entered });
            await db.SaveChangesAsync();
        }
        using var update = await client.PutAsJsonAsync($"/api/animals/{animal}", q with { PaddockId = p, LotId = lot });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var residents = await client.GetFromJsonAsync<CarePage<PaddockResident>>($"/api/paddocks/{p}/residents");
        Assert.Equal(entered, Assert.Single(residents!.Items).ArrivalDate);
        var history = await client.GetFromJsonAsync<CarePage<AnimalMovementRecord>>($"/api/animals/{animal}/movements");
        Assert.Equal(2, history!.Total);
    }
    [Fact]
    public async Task EmployeeCannotReadForeignPaddockOrMoveAcrossFarms()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var foreign = await Create(client, "farms", new FarmRequest("Foreign map", "FOREIGN-MAP"));
        var p = await Create(client, "paddocks", new PaddockRequest(foreign, "Foreign paddock", Capacity: 4));
        var animal = await Create(client, "animals", new AnimalRequest(farm, species, "SCOPED-MOVE", Sex.Female, ProductivePurpose.Milk));
        await Login(client, "employee");
        using var hidden = await client.GetAsync($"/api/paddocks/{p}/residents");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        var page = await client.GetFromJsonAsync<CarePage<PaddockSnapshot>>($"/api/paddocks/page?farmId={foreign}");
        Assert.Empty(page!.Items); Assert.Equal(0, page.Total);
        using var move = await client.PostAsJsonAsync($"/api/animals/{animal}/movements", new AnimalMovementRequest(Guid.NewGuid(), p, null, null, null, "Cross farm"));
        Assert.Equal(HttpStatusCode.Conflict, move.StatusCode);
    }

    [Fact]
    public async Task ResidentsAreFilteredByPhysicalPaddockAndLotBeforePagination()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var p = await Create(client, "paddocks", new PaddockRequest(farm, "Lot filter", Capacity: 5));
        var lot = await Create(client, "lots", new LotRequest(farm, species, "Only this group", ProductivePurpose.Milk));
        for (var i = 0; i < 3; i++) await Create(client, "animals", new AnimalRequest(farm, species, "LOT-FILTER-" + i, Sex.Female, ProductivePurpose.Milk, LotId: lot, PaddockId: p));
        await Create(client, "animals", new AnimalRequest(farm, species, "OUTSIDE-PADDOCK", Sex.Female, ProductivePurpose.Milk, LotId: lot));
        await Create(client, "animals", new AnimalRequest(farm, species, "UNGROUPED", Sex.Female, ProductivePurpose.Milk, PaddockId: p));
        var page = await client.GetFromJsonAsync<CarePage<PaddockResident>>($"/api/paddocks/{p}/residents?lotId={lot}&pageSize=2&page=2");
        Assert.Equal(3, page!.Total); Assert.Equal(2, page.Page); Assert.Single(page.Items);
        var ungrouped = await client.GetFromJsonAsync<CarePage<PaddockResident>>($"/api/paddocks/{p}/residents?ungrouped=true");
        Assert.Equal("UNGROUPED", Assert.Single(ungrouped!.Items).InternalTag);
    }

    [Fact]
    public async Task CorruptCrossFarmReferenceDoesNotExposeForeignAnimal()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var p = await Create(client, "paddocks", new PaddockRequest(farm, "Secure resident map", Capacity: 4));
        var foreign = await Create(client, "farms", new FarmRequest("Foreign residents", "FOREIGN-RESIDENTS"));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Animals.Add(new Animal { FarmId = foreign, SpeciesId = species, InternalTag = "FOREIGN-CORRUPT", PaddockId = p });
            await db.SaveChangesAsync();
        }
        await Login(client, "employee");
        var residents = await client.GetFromJsonAsync<CarePage<PaddockResident>>($"/api/paddocks/{p}/residents");
        Assert.Empty(residents!.Items);
        var map = await client.GetFromJsonAsync<CarePage<PaddockSnapshot>>("/api/paddocks/page?search=Secure resident map");
        Assert.Equal(0, Assert.Single(map!.Items).Occupancy);
    }
}
