using System.Net;
using System.Net.Http.Json;
using Core.Application.Livestock;
using Core.Application.Management;
using Core.Domain.Livestock;

namespace Core.Tests;

public sealed partial class ManagementIntegrationTests
{
    [Fact]
    public async Task MovementDestinationsIncludeActiveUnmappedPaddocksWithRealOccupancy()
    {
        using var factory = Factory(); await Seed(factory);
        using var client = factory.CreateClient(); await Login(client, "admin");
        var (farm, species) = await DemoIds(factory);
        var unmapped = await Create(client, "paddocks", new PaddockRequest(farm, "Unmapped destination", Capacity: 5));
        var inactive = await Create(client, "paddocks", new PaddockRequest(farm, "Inactive destination", IsActive: false));
        await Create(client, "animals", new AnimalRequest(farm, species, "DESTINATION-RESIDENT", Sex.Female, ProductivePurpose.Milk, PaddockId: unmapped));
        var destinations = await client.GetFromJsonAsync<List<PaddockSnapshot>>($"/api/paddocks/destinations?farmId={farm}");
        var actual = Assert.Single(destinations!, item => item.Id == unmapped);
        Assert.Equal(1, actual.Occupancy); Assert.Null(actual.Data.MapX);
        Assert.DoesNotContain(destinations!, item => item.Id == inactive);
        var map = await client.GetFromJsonAsync<List<PaddockSnapshot>>($"/api/paddocks/map?farmId={farm}");
        Assert.DoesNotContain(map!, item => item.Id == unmapped);
        var animals = await client.GetFromJsonAsync<AnimalPageResult>("/api/animals/page?search=DESTINATION-RESIDENT");
        Assert.Equal(unmapped, Assert.Single(animals!.Items).PaddockId);
        using var missing = await client.GetAsync($"/api/paddocks/destinations?farmId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
