using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace Core.Tests;

public sealed partial class ManagementIntegrationTests
{
    [Fact]
    public async Task NotificationsRequireAuthenticationAndExcludeUnassignedFarms()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/notifications")).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var farm = new Farm { Code = "PRIVATE", Name = "Private farm" }; var product = await db.Products.FirstAsync();
        var hidden = new FarmInventory { FarmId = farm.Id, ProductId = product.Id, Stock = 0, MinStock = 2, MaxStock = 20, Location = "PRIVATE" };
        db.Farms.Add(farm); db.FarmInventory.Add(hidden); await db.SaveChangesAsync();
        await Login(client, "employee"); using var reply = await client.GetAsync("/api/notifications"); Assert.Equal(HttpStatusCode.OK, reply.StatusCode);
        using var json = JsonDocument.Parse(await reply.Content.ReadAsStringAsync());
        Assert.DoesNotContain(json.RootElement.GetProperty("items").EnumerateArray(), item => item.GetProperty("farmId").GetGuid() == farm.Id);
        Assert.Contains(reply.Headers.CacheControl!.ToString(), new[] { "no-store" });
    }
    [Fact]
    public async Task IdentificationResolvesTagsAndLabelsButNeverAnotherFarm()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var animal = await db.Animals.FirstAsync();
        foreach (var identifier in new[] { animal.InternalTag, "daw:animal:" + animal.Id })
        {
            using var reply = await client.GetAsync($"/api/animals/identify?farmId={animal.FarmId}&identifier={Uri.EscapeDataString(identifier)}");
            Assert.Equal(HttpStatusCode.OK, reply.StatusCode); using var json = JsonDocument.Parse(await reply.Content.ReadAsStringAsync()); Assert.Equal(animal.Id, json.RootElement.GetProperty("id").GetGuid());
        }
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/animals/identify?farmId={Guid.NewGuid()}&identifier={animal.InternalTag}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/animals/identify?farmId={animal.FarmId}&identifier=daw:animal:invalid")).StatusCode);
    }
    [Fact]
    public async Task NotificationsRecheckRolePermissionsOnTheServer()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleIds = await db.UserRoles.Where(r => r.UserId == db.Users.Where(u => u.UserName == "employee").Select(u => u.Id).First()).Select(r => r.RoleId).ToListAsync();
        var grants = await db.RolePermissions.Where(r => roleIds.Contains(r.RoleId) && r.Permission.Name == "inventory.list").ToListAsync();
        db.RolePermissions.RemoveRange(grants); await db.SaveChangesAsync();
        await Login(client, "employee"); using var reply = await client.GetAsync("/api/notifications"); Assert.Equal(HttpStatusCode.OK, reply.StatusCode);
        using var json = JsonDocument.Parse(await reply.Content.ReadAsStringAsync()); Assert.DoesNotContain(json.RootElement.GetProperty("items").EnumerateArray(), item => item.GetProperty("kind").GetString() == "stock");
    }
}
