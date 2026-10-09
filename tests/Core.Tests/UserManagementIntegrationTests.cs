using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Application.Security;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace Core.Tests;
public sealed class UserManagementIntegrationTests
{
    private const string AdminPassword="Admin123!Test";
    [Fact] public async Task ManagedAccountPersistsAssignmentsAndInvalidatesAccessAndRefreshOnDeactivation()
    {
        using var factory=CreateFactory();await SeedAsync(factory);using var admin=factory.CreateClient();admin.DefaultRequestHeaders.Authorization=new("Bearer",await LoginAsync(admin,"admin",AdminPassword));
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var farm=await db.Farms.FirstAsync();
        var draft=new UserWriteRequest("managed","managed@example.test","Managed operator","Employee",[farm.Id],[],InitialPassword:"Operator123!Test");
        var created=await admin.PostAsJsonAsync("/api/admin/users",draft);Assert.Equal(HttpStatusCode.Created,created.StatusCode);var user=(await created.Content.ReadFromJsonAsync<ManagedUser>())!;Assert.Equal(farm.Id,Assert.Single(user.FarmIds));
        using var employee=factory.CreateClient();var login=await employee.PostAsJsonAsync("/api/auth/login",new {username="managed",password="Operator123!Test"});var session=await login.Content.ReadFromJsonAsync<AuthResponse>();employee.DefaultRequestHeaders.Authorization=new("Bearer",session!.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden,(await employee.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await employee.GetAsync("/api/animals/page")).StatusCode);
        var update=await admin.PutAsJsonAsync("/api/admin/users/"+user.Id,draft with {InitialPassword=null,ExpectedVersion=user.Version,IsActive=false});Assert.Equal(HttpStatusCode.OK,update.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await employee.GetAsync("/api/animals/page")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await employee.PostAsJsonAsync("/api/auth/refresh",new {refreshToken=session.RefreshToken})).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await employee.PostAsJsonAsync("/api/auth/login",new {username="managed",password="Operator123!Test"})).StatusCode);
        var hash=(await db.Users.AsNoTracking().SingleAsync(u=>u.Id==user.Id)).PasswordHash;Assert.NotEqual(draft.InitialPassword,hash);
        Assert.True(await db.AuditLogs.AnyAsync(a=>a.EntityId==user.Id.ToString()&&a.Action=="UserUpdated"));
        Assert.False(await db.AuditLogs.AnyAsync(a=>a.EntityId==user.Id.ToString()&&a.NewValues!.Contains("Operator123!Test")));
    }
    [Fact] public async Task PasswordResetRejectsOldPasswordAndTokensAndDetectsStaleProfile()
    {
        using var factory=CreateFactory();await SeedAsync(factory);using var admin=factory.CreateClient();admin.DefaultRequestHeaders.Authorization=new("Bearer",await LoginAsync(admin,"admin",AdminPassword));
        var draft=new UserWriteRequest("resetuser","reset@example.test","Reset operator","SoloLectura",[],[],InitialPassword:"OldPassword123!");
        var response=await admin.PostAsJsonAsync("/api/admin/users",draft);var user=(await response.Content.ReadFromJsonAsync<ManagedUser>())!;
        using var employee=factory.CreateClient();employee.DefaultRequestHeaders.Authorization=new("Bearer",await LoginAsync(employee,"resetuser","OldPassword123!"));
        Assert.Equal(HttpStatusCode.Conflict,(await admin.PutAsJsonAsync("/api/admin/users/"+user.Id,draft with {InitialPassword=null,ExpectedVersion="old"})).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await admin.PostAsJsonAsync("/api/admin/users/"+user.Id+"/password",new PasswordResetRequest("NewPassword123!",user.Version))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await employee.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await employee.PostAsJsonAsync("/api/auth/login",new {username="resetuser",password="OldPassword123!"})).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await employee.PostAsJsonAsync("/api/auth/login",new {username="resetuser",password="NewPassword123!"})).StatusCode);
    }
    [Fact] public async Task UnknownReferencesAndProtectedSuperuserCannotBeChanged()
    {
        using var factory=CreateFactory();await SeedAsync(factory);using var admin=factory.CreateClient();admin.DefaultRequestHeaders.Authorization=new("Bearer",await LoginAsync(admin,"admin",AdminPassword));
        var draft=new UserWriteRequest("badfarm","bad@example.test","Bad reference","Employee",[Guid.NewGuid()],[],InitialPassword:"Password123!");Assert.Equal(HttpStatusCode.BadRequest,(await admin.PostAsJsonAsync("/api/admin/users",draft)).StatusCode);
        var page=await (await admin.GetAsync("/api/admin/users?search=admin")).Content.ReadFromJsonAsync<Core.Application.Livestock.CarePage<ManagedUser>>();var system=page!.Items.Single(u=>u.Username=="admin");
        Assert.Equal(HttpStatusCode.Conflict,(await admin.PutAsJsonAsync("/api/admin/users/"+system.Id,draft with {Username="admin",Email="admin@daw.local",InitialPassword=null,FarmIds=[],ExpectedVersion=system.Version,IsActive=false})).StatusCode);
    }
    private static async Task SeedAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
    }

    private static async Task<string?> LoginAsync(HttpClient client, string username, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accessToken").GetString();
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = "auth-tests-" + Guid.NewGuid();

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DisableHttpsRedirection"] = "true",
                    ["Jwt:Key"] = new string('t', 64),
                    ["Seed:AdminUsername"] = "admin",
                    ["Seed:AdminEmail"] = "admin@daw.local",
                    ["Seed:AdminPassword"] = AdminPassword,
                    ["Storage:RootPath"] = Path.Combine(Path.GetTempPath(), "daw-uploads-tests", Guid.NewGuid().ToString("N"))
                }));

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName));
            });
        });
    }
}
