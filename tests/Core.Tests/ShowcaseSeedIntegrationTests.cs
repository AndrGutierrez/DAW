using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Application.Operations;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests;

public sealed partial class ManagementIntegrationTests
{
    private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> ShowcaseFactory(string? password = "Test123!Pass") =>
        Factory().WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Seed:DemoPassword"] = password })));

    [Fact]
    public async Task ExpandedDemoBuildsConsistentHistoriesAndCalculatedIndicators()
    {
        using var factory = ShowcaseFactory(); await Seed(factory);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var result = await ShowcaseSeeder.SeedAsync(scope.ServiceProvider);
        Assert.False(result.Replayed); Assert.Equal(3, result.Farms); Assert.Equal(180, result.Animals); Assert.Equal(7, result.Users);
        var farmIds = await db.Farms.Where(f => f.Code.StartsWith("MEGA-")).Select(f => f.Id).ToArrayAsync();
        Assert.Equal(18, await db.Paddocks.CountAsync(p => farmIds.Contains(p.FarmId)));
        Assert.Equal(15, await db.Lots.CountAsync(p => farmIds.Contains(p.FarmId)));
        Assert.Equal(36, result.Inventory); Assert.Equal(360, result.Movements);
        Assert.True(result.Weights > 1000); Assert.True(result.Production > 2100); Assert.True(result.ClinicalEvents > 300);
        var animals = await db.Animals.Where(a => farmIds.Contains(a.FarmId)).ToListAsync();
        var weights = await db.WeightRecords.Where(w => farmIds.Contains(w.FarmId)).ToListAsync();
        Assert.All(weights, w => { Assert.True(w.WeightKg > 0); Assert.True(w.Date >= animals.Single(a => a.Id == w.AnimalId).BirthDate); });
        Assert.Equal(24, animals.Count(a => a.DamId != null && a.SireId != null));
        Assert.All(animals.Where(a => a.DamId != null), a =>
        {
            var dam = animals.Single(d => d.Id == a.DamId); var sire = animals.Single(d => d.Id == a.SireId);
            Assert.Equal(a.FarmId, dam.FarmId); Assert.Equal(a.SpeciesId, dam.SpeciesId); Assert.Equal(Sex.Female, dam.Sex); Assert.True(dam.BirthDate < a.BirthDate);
            Assert.Equal(a.FarmId, sire.FarmId); Assert.Equal(a.SpeciesId, sire.SpeciesId); Assert.Equal(Sex.Male, sire.Sex);
        });
        var milk = await db.AnimalProduction.Where(p => farmIds.Contains(p.FarmId) && p.ProductType == AnimalProductType.Milk).ToListAsync();
        var treatments = await db.HealthEvents.OfType<Treatment>().Where(t => farmIds.Contains(t.FarmId)).ToListAsync();
        Assert.All(treatments, t => Assert.DoesNotContain(milk, m => m.AnimalId == t.AnimalId && m.Date >= t.Date && m.Date <= t.WithdrawalEndDate));
        var moves = await db.AnimalMovements.Where(m => farmIds.Contains(m.FarmId)).ToListAsync();
        Assert.All(animals, a =>
        {
            var chain = moves.Where(m => m.AnimalId == a.Id).OrderBy(m => m.Date).ThenBy(m => m.CreatedAt).ToArray();
            Assert.Equal(2, chain.Length); Assert.Null(chain[0].FromPaddockId); Assert.Equal(chain[0].ToPaddockId, chain[1].FromPaddockId);
            Assert.Equal(a.PaddockId, chain[1].ToPaddockId); Assert.Equal(a.LotId, chain[1].ToLotId);
        });
        var reader = scope.ServiceProvider.GetRequiredService<IOperationsReader>();
        var period = new PeriodQuery(result.AnchorDate.AddDays(-29), result.AnchorDate);
        var inputs = await reader.AnalyticsAsync(period, farmIds, default);
        var analytics = AnalyticsService.Calculate(period, inputs);
        Assert.Equal(30, analytics.MilkByDay.Count); Assert.True(analytics.MilkByDay.Sum(p => p.Value) > 10000);
        Assert.NotNull(analytics.Reproduction.PregnancyPercent); Assert.True(analytics.Reproduction.PendingServices > 0); Assert.True(analytics.Calvings > 0);
        Assert.True(analytics.Critical > 0); Assert.All(analytics.Stock, s => Assert.NotNull(s.Rotation));
        foreach (var inventory in await db.FarmInventory.Where(i => farmIds.Contains(i.FarmId)).ToListAsync())
        {
            var movements = await db.StockMovements.Where(m => m.FarmId == inventory.FarmId && m.ProductId == inventory.ProductId).ToListAsync();
            var baseline = Assert.Single(movements, m => m.ReferenceType == "OpeningBalance");
            Assert.Equal(inventory.Stock, baseline.Quantity + movements.Where(m => m.Type != StockMovementType.Adjustment).Sum(m => m.Type == StockMovementType.In ? m.Quantity : -m.Quantity));
        }
        Assert.False(await db.AuditLogs.AnyAsync(a => a.NewValues!.Contains("Test123!Pass") || a.NewValues!.Contains("PasswordHash")));
        Assert.Single(await db.AuditLogs.Where(a => a.EntityName == "ShowcaseSeed").ToArrayAsync());
    }

    [Fact]
    public async Task ExpandedDemoReplayRetainsUserEditsArchivesAndTheOriginalDate()
    {
        using var factory = ShowcaseFactory(); await Seed(factory);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var original = await ShowcaseSeeder.SeedAsync(scope.ServiceProvider);
        var archived = await db.Animals.FirstAsync(a => a.InternalTag.StartsWith("MEGA-")); archived.MarkDeleted(null);
        var user = await db.Users.SingleAsync(u => u.UserName == "demo.operador"); user.IsActive = false; user.FullName = "Changed by administrator";
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var token = await manager.GeneratePasswordResetTokenAsync(user); Assert.True((await manager.ResetPasswordAsync(user, token, "Changed123!Pass")).Succeeded);
        await db.SaveChangesAsync();
        var counts = new[] { await db.Animals.IgnoreQueryFilters().CountAsync(), await db.AuditLogs.CountAsync(), await db.AnimalProduction.CountAsync(), await db.WeightRecords.CountAsync() };
        var replay = await ShowcaseSeeder.SeedAsync(scope.ServiceProvider, original.AnchorDate.AddDays(10));
        Assert.True(replay.Replayed); Assert.Equal(original.AnchorDate, replay.AnchorDate);
        Assert.Equal(counts, new[] { await db.Animals.IgnoreQueryFilters().CountAsync(), await db.AuditLogs.CountAsync(), await db.AnimalProduction.CountAsync(), await db.WeightRecords.CountAsync() });
        Assert.False(await db.Animals.AnyAsync(a => a.Id == archived.Id)); Assert.False(user.IsActive); Assert.Equal("Changed by administrator", user.FullName);
        Assert.True(await manager.CheckPasswordAsync(user, "Changed123!Pass")); Assert.False(await manager.CheckPasswordAsync(user, "Test123!Pass"));
    }

    [Fact]
    public async Task EveryDemoRoleCanLogInAndIsRestrictedToItsFarmsAndOperations()
    {
        using var factory = ShowcaseFactory(); await Seed(factory);
        using var scope = factory.Services.CreateScope(); await ShowcaseSeeder.SeedAsync(scope.ServiceProvider);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hiddenFarm = await db.Farms.SingleAsync(f => f.Code == "MEGA-SIERRA");
        var hiddenAnimal = await db.Animals.FirstAsync(a => a.FarmId == hiddenFarm.Id);
        foreach (var account in ShowcaseSeeder.Accounts)
        {
            using var client = factory.CreateClient(); await Login(client, account.Username);
            var administrator = account.Role is "Admin" or "Administrador";
            using var animals = await client.GetAsync("/api/animals/page?pageSize=100"); Assert.Equal(HttpStatusCode.OK, animals.StatusCode);
            using var json = JsonDocument.Parse(await animals.Content.ReadAsStringAsync());
            Assert.Equal(administrator ? 188 : account.FarmIndexes.Length * 60, json.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(administrator ? HttpStatusCode.OK : HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/users")).StatusCode);
            Assert.Equal(administrator ? HttpStatusCode.OK : HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/auditlogs")).StatusCode);
            Assert.Equal(account.FarmIndexes.Contains(2) || administrator ? HttpStatusCode.OK : HttpStatusCode.NotFound,
                (await client.GetAsync("/api/animals/" + hiddenAnimal.Id)).StatusCode);
            Assert.Equal(administrator ? HttpStatusCode.BadRequest : HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/products", new { })).StatusCode);
            var assignedFarm = await db.Farms.SingleAsync(f => f.Code == new[] { "MEGA-VALLE", "MEGA-LLANO", "MEGA-SIERRA" }[account.FarmIndexes[0]]);
            var animal = await db.Animals.FirstAsync(a => a.FarmId == assignedFarm.Id && a.Status == AnimalStatus.Active);
            var weight = await client.PostAsJsonAsync("/api/weights", new { farmId = assignedFarm.Id, animalId = animal.Id, date = DateOnly.FromDateTime(DateTime.UtcNow), weightKg = 500 });
            Assert.Equal(account.Role == "SoloLectura" ? HttpStatusCode.Forbidden : HttpStatusCode.Created, weight.StatusCode);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("weak")]
    public async Task MissingOrWeakDemoPasswordDoesNotImportData(string? password)
    {
        using var factory = ShowcaseFactory(password); await Seed(factory);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => ShowcaseSeeder.SeedAsync(scope.ServiceProvider));
        Assert.False(await db.Farms.AnyAsync(f => f.Code.StartsWith("MEGA-"))); Assert.False(await db.Users.AnyAsync(u => u.UserName!.StartsWith("demo.")));
    }

    [Fact]
    public async Task IdentifierCollisionDoesNotOverwriteOrRestoreArchivedFarm()
    {
        using var factory = ShowcaseFactory(); await Seed(factory);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var farm = new Farm { Code = "MEGA-VALLE", Name = "Existing archived farm" }; farm.MarkDeleted(null); db.Farms.Add(farm); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => ShowcaseSeeder.SeedAsync(scope.ServiceProvider));
        Assert.Equal("Existing archived farm", farm.Name); Assert.True(farm.IsDeleted);
        Assert.False(await db.Users.AnyAsync(u => u.UserName!.StartsWith("demo.")));
    }
}
