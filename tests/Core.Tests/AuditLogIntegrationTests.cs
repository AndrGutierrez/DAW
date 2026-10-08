using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Application.Auditing;
using Core.Application.Livestock;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Tests;

public sealed partial class ManagementIntegrationTests
{
    [Fact]
    public async Task AuditFiltersArePaginatedOrderedAndExcludePayloadsUntilDetailIsRequested()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(x => x.UserName == "employee"); var farm = await db.Farms.FirstAsync();
        var day = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc); var affected = Guid.NewGuid();
        var rows = Enumerable.Range(0, 3).Select(i => new AuditLog
        {
            UserId = user.Id, FarmId = farm.Id, EntityName = "Animal", EntityId = affected.ToString(),
            Action = "Modified", OccurredAt = day.AddHours(i), IpAddress = "192.0.2.10",
            OldValues = "{\"Name\":\"Old\",\"Nested\":{\"PasswordHash\":\"sensitive\"}}",
            NewValues = "{\"Name\":\"New\",\"Tokens\":[\"private\"]}"
        }).ToArray();
        db.AuditLogs.AddRange(rows);
        db.AuditLogs.Add(new AuditLog { EntityName = "Animal", EntityId = affected.ToString(), Action = "Modified", OccurredAt = day.AddDays(1) });
        await db.SaveChangesAsync();
        var query = $"/api/admin/auditlogs?from=2026-09-20&to=2026-09-20&actor=EMPLOYEE&farmId={farm.Id}&entityName=Animal&action=Modified&search={affected}&pageSize=2";
        var page = await client.GetFromJsonAsync<CarePage<AuditEntry>>(query);
        Assert.NotNull(page); Assert.Equal(3, page.Total); Assert.Equal(rows[2].Id, page.Items[0].Id); Assert.Equal(2, page.Items.Count);
        var next = await client.GetFromJsonAsync<CarePage<AuditEntry>>(query + "&page=2"); Assert.Equal(rows[0].Id, Assert.Single(next!.Items).Id);
        using var summary = JsonDocument.Parse(await client.GetStringAsync(query));
        Assert.False(summary.RootElement.GetProperty("items")[0].TryGetProperty("oldValues", out _));
        var detail = await client.GetFromJsonAsync<AuditDetail>("/api/admin/auditlogs/" + rows[0].Id);
        Assert.Equal("192.0.2.10", detail!.IpAddress); Assert.Contains("[redacted]", detail.OldValues!); Assert.Contains("[redacted]", detail.NewValues!);
        Assert.DoesNotContain("sensitive", detail.OldValues!); Assert.DoesNotContain("private", detail.NewValues!);
        var options = await client.GetFromJsonAsync<AuditOptions>("/api/admin/auditlogs/options"); Assert.Contains("Animal", options!.Entities); Assert.Contains("Modified", options.Actions);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/admin/auditlogs/" + Guid.NewGuid())).StatusCode);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=1000001")]
    [InlineData("from=2026-10-08&to=2026-10-07")]
    public async Task AuditRejectsInvalidQueries(string query)
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/admin/auditlogs?" + query)).StatusCode);
    }

    [Fact]
    public async Task AuditRequiresAdminRoleAndPermissionEvenIfAnEmployeeHasADirectAuditGrant()
    {
        using var factory = Factory(); await Seed(factory); using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/admin/auditlogs")).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var employee = await db.Users.SingleAsync(x => x.UserName == "employee");
        var auditPermission = await db.Permissions.SingleAsync(x => x.Name == "auditlogs.list");
        db.UserPermissions.Add(new UserPermission { UserId = employee.Id, PermissionId = auditPermission.Id }); await db.SaveChangesAsync();
        using var client = factory.CreateClient(); await Login(client, "employee");
        foreach (var route in new[] { "", "/options", "/" + Guid.NewGuid() })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/auditlogs" + route)).StatusCode);
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var restricted = new ApplicationUser { UserName = "audit-admin", Email = "audit-admin@example.test", IsActive = true, EmailConfirmed = true };
        Assert.True((await manager.CreateAsync(restricted, "Test123!Pass")).Succeeded); Assert.True((await manager.AddToRoleAsync(restricted, "Admin")).Succeeded);
        var adminRole = await db.Roles.SingleAsync(r => r.Name == "Admin");
        db.RolePermissions.RemoveRange(await db.RolePermissions.Where(p => p.RoleId == adminRole.Id && p.Permission.Name.StartsWith("auditlogs.")).ToListAsync()); await db.SaveChangesAsync();
        await Login(client, "audit-admin"); Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/auditlogs")).StatusCode);
    }

    [Fact]
    public async Task AuditKeepsOrphanedHistoryAndHasNoMutationEndpoint()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = new AuditLog { UserId = Guid.NewGuid(), FarmId = Guid.NewGuid(), Action = "Deleted", EntityName = "Farm", EntityId = Guid.NewGuid().ToString(), OldValues = "{\"Name\":\"Former farm\"}" };
        db.AuditLogs.Add(row); await db.SaveChangesAsync();
        var entry = await client.GetFromJsonAsync<AuditDetail>("/api/admin/auditlogs/" + row.Id); Assert.NotNull(entry); Assert.Null(entry.Entry.ActorName); Assert.Null(entry.Entry.FarmName); Assert.Contains("Former farm", entry.OldValues!);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.DeleteAsync("/api/admin/auditlogs/" + row.Id)).StatusCode);
        Assert.True(await db.AuditLogs.AnyAsync(x => x.Id == row.Id));
    }

    [Fact]
    public async Task ConfiguredTrustedProxySuppliesClientIpForAccessEvents()
    {
        using var factory = Factory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => Microsoft.Extensions.Configuration.MemoryConfigurationBuilderExtensions.AddInMemoryCollection(config,
                new Dictionary<string, string?> { ["ReverseProxy:KnownProxies:0"] = "192.0.2.20" }));
            builder.ConfigureServices(services => services.AddTransient<Microsoft.AspNetCore.Hosting.IStartupFilter, AuditPeerFilter>());
        });
        await Seed(factory); using var client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.10"); await Login(client, "admin");
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("203.0.113.10", (await db.AuditLogs.SingleAsync(x => x.Action == "LoginSucceeded")).IpAddress);
    }

    [Fact]
    public async Task AnimalWithMovementHistoryCannotBeHardDeleted()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        var (farm, species) = await DemoIds(factory); var id = await Create(client, "animals", new AnimalRequest(farm, species, "AUDIT-MOVE", Sex.Male, ProductivePurpose.Meat));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AnimalMovements.Add(new AnimalMovement { AnimalId = id, Date = DateOnly.FromDateTime(DateTime.UtcNow), Reason = "Recorded transfer" }); await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync("/api/animals/" + id)).StatusCode);
        Assert.True(await db.Animals.AnyAsync(x => x.Id == id)); Assert.True(await db.AnimalMovements.AnyAsync(x => x.AnimalId == id));
    }

    [Fact]
    public async Task MutationAndUserManagementAuditsUseTransportIpRatherThanAnUntrustedHeader()
    {
        using var factory = Factory().WithWebHostBuilder(builder => builder.ConfigureServices(services => services.AddTransient<Microsoft.AspNetCore.Hosting.IStartupFilter, AuditPeerFilter>()));
        await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin"); client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.99");
        var (farm, species) = await DemoIds(factory); var id = await Create(client, "animals", new AnimalRequest(farm, species, "AUDIT-IP", Sex.Male, ProductivePurpose.Meat));
        var result = await client.PostAsJsonAsync("/api/admin/users", new UserWriteRequest("audit-ip-user", "audit-ip@example.test", "Audit IP user", "Employee", [farm], [], InitialPassword: "Test123!Pass")); Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("192.0.2.20", (await db.AuditLogs.SingleAsync(x => x.EntityId == id.ToString())).IpAddress);
        Assert.Equal("192.0.2.20", (await db.AuditLogs.SingleAsync(x => x.Action == "UserCreated")).IpAddress);
    }
    [Fact]
    public async Task PhotoCreationAndDeletionAreAuditedWithActorFarmAndIpWithoutImageBytes()
    {
        using var factory = Factory().WithWebHostBuilder(builder => builder.ConfigureServices(services => services.AddTransient<Microsoft.AspNetCore.Hosting.IStartupFilter, AuditPeerFilter>()));
        await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var animal = await db.Animals.FirstAsync();
        using var form = new MultipartFormDataContent(); var file = new ByteArrayContent(TestImages.Png); file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png"); form.Add(file, "file", "audit-photo.png");
        using var uploaded = await client.PostAsync($"/api/animals/{animal.Id}/photo", form); Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        using var response = JsonDocument.Parse(await uploaded.Content.ReadAsStringAsync()); var photoId = response.RootElement.GetProperty("photoId").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/animals/{animal.Id}/photos/{photoId}")).StatusCode);
        var events = await db.AuditLogs.Where(x => x.EntityName == "AnimalPhoto" && x.EntityId == photoId.ToString()).ToListAsync(); Assert.Equal(2, events.Count);
        Assert.All(events, row => { Assert.NotNull(row.UserId); Assert.Equal(animal.FarmId, row.FarmId); Assert.Equal("192.0.2.20", row.IpAddress); });
        var added = Assert.Single(events, x => x.Action == "Added"); var deleted = Assert.Single(events, x => x.Action == "Deleted");
        Assert.Null(added.OldValues); Assert.Contains("image/png", added.NewValues!); Assert.Null(deleted.NewValues); Assert.Equal(added.NewValues, deleted.OldValues);
        Assert.DoesNotContain(Convert.ToBase64String(TestImages.Png), added.NewValues!);
    }

    [Fact]
    public async Task RolePermissionChangesAreAuditedOncePerActualChange()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var role = await db.Roles.SingleAsync(x => x.Name == "SoloLectura"); var permission = await db.Permissions.SingleAsync(x => x.Name == "weights.create");
        var endpoint = $"/api/admin/roles/{role.Id}/permissions/{permission.Id}";
        for (var i = 0; i < 2; i++) Assert.Equal(HttpStatusCode.OK, (await client.PostAsync(endpoint, null)).StatusCode);
        for (var i = 0; i < 2; i++) Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync(endpoint)).StatusCode);
        var events = await db.AuditLogs.Where(x => x.EntityName == "RolePermission" && x.EntityId == role.Id.ToString()).ToListAsync(); Assert.Equal(2, events.Count);
        Assert.All(events, x => Assert.NotNull(x.UserId)); Assert.Single(events, x => x.Action == "RolePermissionGranted"); Assert.Single(events, x => x.Action == "RolePermissionRevoked");
        using var before = JsonDocument.Parse(events.Single(x => x.Action == "RolePermissionGranted").OldValues!);
        Assert.False(before.RootElement.GetProperty("Assigned").GetBoolean());
        Assert.Contains("weights.create", events[0].NewValues!);
    }

}

internal sealed class AuditPeerFilter : Microsoft.AspNetCore.Hosting.IStartupFilter
{
    public Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> Configure(Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> next) => app =>
    {
        Microsoft.AspNetCore.Builder.UseExtensions.Use(app, (context, continuation) =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.20");
            return continuation(context);
        });
        next(app);
    };
}
