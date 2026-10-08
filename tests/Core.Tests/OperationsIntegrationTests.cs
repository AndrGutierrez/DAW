using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Application.Operations;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace Core.Tests;
public sealed partial class ManagementIntegrationTests
{
    [Fact]
    public async Task InventoryMovementsReplayAndPreventAbsoluteStockBypass()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var inventory = await db.FarmInventory.FirstAsync();
        var payload = new StockRequest(Guid.NewGuid(), StockMovementType.Out, 0.0001m, inventory.Stock, "Precision test");
        var url = $"/api/inventory/{inventory.Id}/movements";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(url, payload)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(url, payload)).StatusCode);
        db.ChangeTracker.Clear(); var saved = await db.FarmInventory.SingleAsync(i => i.Id == inventory.Id); Assert.Equal(inventory.Stock - 0.0001m, saved.Stock);
        Assert.Equal(2, await db.StockMovements.CountAsync(m => m.FarmId == inventory.FarmId && m.ProductId == inventory.ProductId));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/inventory/{inventory.Id}", new { saved.FarmId, saved.ProductId, stock = 100, saved.MinStock, saved.MaxStock, saved.Location })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/inventory/{inventory.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/inventory/{inventory.Id}", new { saved.FarmId, saved.ProductId, saved.Stock, minStock = 1, saved.MaxStock, saved.Location })).StatusCode);
    }
    [Fact]
    public async Task EmployeeCannotReadDashboardEvenWithInventoryPermission()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "employee"); var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/inventory/page")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/analytics/overview?from={date}&to={date}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/products/{Guid.NewGuid()}")).StatusCode);
    }
    [Fact]
    public async Task ReportAndInventoryFiltersDoNotLeakUnassignedFarmData()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var farm = new Farm { Name = "Hidden", Code = "HIDDEN" }; db.Farms.Add(farm); await db.SaveChangesAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        await Login(client, "employee");
        using var report = await client.GetAsync($"/api/reports/production/export?from={date}&to={date}&farmId={farm.Id}"); Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        using var json = JsonDocument.Parse(await report.Content.ReadAsStringAsync()); Assert.Equal(0, json.RootElement.GetProperty("records").GetProperty("total").GetInt32());
        using var inventory = await client.GetAsync($"/api/inventory/page?farmId={farm.Id}"); using var stock = JsonDocument.Parse(await inventory.Content.ReadAsStringAsync()); Assert.Equal(0, stock.RootElement.GetProperty("total").GetInt32());
    }
    [Fact]
    public async Task ReportingRejectsInvalidPeriodsAndOversizedPages()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin"); var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        foreach (var route in new[] { "/api/reports/clinical", "/api/reports/production", "/api/analytics/overview" }) Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(route + $"?from={date}&to=2000-01-01")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/reports/production?from={date}&to={date}&pageSize=10000")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/inventory/page?pageSize=101")).StatusCode);
    }
    [Fact]
    public async Task SupplyReferencesPreventAnimalAndFarmDeletion()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var inventory = await db.FarmInventory.FirstAsync();
        var animal = await db.Animals.FirstAsync(a => a.FarmId == inventory.FarmId && a.Status == AnimalStatus.Active);
        var q = new StockRequest(Guid.NewGuid(), StockMovementType.Out, 1, inventory.Stock, "Linked supply", animal.Id);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync($"/api/inventory/{inventory.Id}/movements", q)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/animals/{animal.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/farms/{inventory.FarmId}")).StatusCode);
    }
    [Fact]
    public async Task OversizedExportIsRejectedInsteadOfSilentlyTruncatingRecords()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var animal = await db.Animals.FirstAsync(); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.AnimalProduction.AddRange(Enumerable.Range(0, 10001).Select(_ => new AnimalProduction { FarmId = animal.FarmId, AnimalId = animal.Id, Date = today, ProductType = AnimalProductType.Other, Method = ProductionMethod.Collection, Quantity = 1, Unit = MeasurementUnit.Unit, OperationId = Guid.NewGuid() })); await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync($"/api/reports/production/export?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}")).StatusCode);
    }

}
