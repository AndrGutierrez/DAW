using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Core.Application.Security;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Tests;

public sealed class AuthenticationIntegrationTests
{
    private const string AdminPassword = "Admin123!Test";

    [Fact]
    public async Task SeededAdminCanLoginAndReadRoles()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);

        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "admin", AdminPassword);
        Assert.False(string.IsNullOrWhiteSpace(token));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == "admin@daw.local");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var roles = await client.GetAsync("/api/admin/roles");
        Assert.Equal(HttpStatusCode.OK, roles.StatusCode);
    }

    [Fact]
    public async Task RegistrationPersistsDifferentHashesForTheSamePasswordAndVerifiesCredentials()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);
        using var client = factory.CreateClient();
        var prefix = "salt" + Guid.NewGuid().ToString("N")[..8];
        const string password = "Shared123!Pass";
        foreach (var suffix in new[] { "one", "two" })
        {
            using var registration = await client.PostAsJsonAsync("/api/auth/register", new
            {
                username = prefix + suffix,
                email = $"{prefix}{suffix}@daw.local",
                password,
                fullName = "Hash verification"
            });
            Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        }

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = await db.Users.Where(user => user.UserName!.StartsWith(prefix)).ToListAsync();
        Assert.Equal(2, users.Count);
        Assert.NotEqual(users[0].PasswordHash, users[1].PasswordHash);
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var user in users)
        {
            Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
            Assert.NotEqual(password, user.PasswordHash);
            Assert.True(await manager.CheckPasswordAsync(user, password));
            Assert.False(await manager.CheckPasswordAsync(user, "Wrong123!Pass"));
        }
    }

    [Fact]
    public async Task UserWithoutPermissionIsForbidden()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);

        using var client = factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        using var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "operario" + suffix,
            email = $"operario{suffix}@daw.local",
            password = "User123!Test",
            fullName = "Operario"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        using var registered = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        var token = registered.RootElement.GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var roles = await client.GetAsync("/api/admin/roles");
        Assert.Equal(HttpStatusCode.Forbidden, roles.StatusCode);
    }

    [Fact]
    public async Task SeededAdminCanListAndGetAnimals()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);

        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "admin", AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var list = await client.GetAsync("/api/animals");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        using var animals = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.True(animals.RootElement.GetArrayLength() >= 6);

        var first = animals.RootElement[0];
        var animalId = first.GetProperty("id").GetGuid();
        Assert.False(string.IsNullOrWhiteSpace(first.GetProperty("internalTag").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(first.GetProperty("species").GetString()));

        using var detail = await client.GetAsync($"/api/animals/{animalId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);

        using var animal = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal(animalId, animal.RootElement.GetProperty("id").GetGuid());
        Assert.False(string.IsNullOrWhiteSpace(animal.RootElement.GetProperty("farm").GetString()));
    }

    [Fact]
    public async Task AnonymousCannotListAnimals()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/animals");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserWithoutPermissionCannotListAnimals()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);

        using var client = factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        using var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "user" + suffix,
            email = $"user{suffix}@daw.local",
            password = "User123!Test",
            fullName = "Usuario de campo"
        });

        using var registered = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        var token = registered.RootElement.GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.GetAsync("/api/animals");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MeReturnsRolesAndPermissions()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);

        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "admin", AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("admin", user.RootElement.GetProperty("username").GetString());
        var permissions = user.RootElement.GetProperty("permissions").EnumerateArray().Select(item => item.GetString()).ToList();
        Assert.Contains("animals.list", permissions);
    }

    [Fact]
    public async Task RefreshReturnsNewTokens()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);

        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = AdminPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using var auth = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var refreshToken = auth.RootElement.GetProperty("refreshToken").GetString();

        using var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

        using var refreshed = JsonDocument.Parse(await refresh.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(refreshed.RootElement.GetProperty("accessToken").GetString()));
        Assert.NotEqual(refreshToken, refreshed.RootElement.GetProperty("refreshToken").GetString());

        using var replay = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task LoginAcceptsAnEmailAddress()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);
        using var client = factory.CreateClient();

        var token = await LoginAsync(client, "admin@daw.local", AdminPassword);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("admin", user.RootElement.GetProperty("username").GetString());
    }

    [Fact]
    public async Task InactiveAccountCannotRefreshOrReadItsProfile()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);
        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = AdminPassword });
        using var auth = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = auth.RootElement.GetProperty("accessToken").GetString();
        var refreshToken = auth.RootElement.GetProperty("refreshToken").GetString();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var admin = await db.Users.SingleAsync(user => user.UserName == "admin");
            admin.IsActive = false;
            await db.SaveChangesAsync();
        }

        using var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        using var animals = await client.GetAsync("/api/animals");
        Assert.Equal(HttpStatusCode.Forbidden, animals.StatusCode);
    }

    [Fact]
    public async Task ReadOnlyUserCanReadAssignedFarmButCannotChangePhotosOrRolePermissions()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);
        Guid animalId;
        Guid photoId;
        Guid roleId;
        Guid permissionId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = "reader", Email = "reader@daw.local", FullName = "Reader" };
            Assert.True((await userManager.CreateAsync(user, "User123!Test")).Succeeded);
            Assert.True((await userManager.AddToRoleAsync(user, PermissionCatalog.ReadOnlyRole)).Succeeded);
            var animal = await db.Animals.FirstAsync();
            animalId = animal.Id;
            db.UserFarms.Add(new UserFarm { UserId = user.Id, FarmId = animal.FarmId });
            var photo = new AnimalPhoto
            {
                FarmId = animal.FarmId,
                AnimalId = animal.Id,
                Url = "/uploads/existing.png",
                FileName = "existing.png"
            };
            db.AnimalPhotos.Add(photo);
            await db.SaveChangesAsync();
            photoId = photo.Id;
            roleId = (await db.Roles.SingleAsync(role => role.Name == PermissionCatalog.ReadOnlyRole)).Id;
            permissionId = (await db.Permissions.SingleAsync(permission => permission.Name == "photos.create")).Id;
        }
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "reader", "User123!Test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var detail = await client.GetAsync($"/api/animals/{animalId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(TestImages.Png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "photo.png");
        using var upload = await client.PostAsync($"/api/animals/{animalId}/photo", form);
        Assert.Equal(HttpStatusCode.Forbidden, upload.StatusCode);
        using var delete = await client.DeleteAsync($"/api/animals/{animalId}/photos/{photoId}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        using var grant = await client.PostAsync($"/api/admin/roles/{roleId}/permissions/{permissionId}", null);
        Assert.Equal(HttpStatusCode.Forbidden, grant.StatusCode);

        using var verification = factory.Services.CreateScope();
        var verificationDb = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await verificationDb.AnimalPhotos.AnyAsync(photo => photo.Id == photoId));
        Assert.False(await verificationDb.RolePermissions.AnyAsync(link =>
            link.RoleId == roleId && link.PermissionId == permissionId));
    }

    [Fact]
    public async Task RegistrationValidationReturnsProblemDetailsWithFieldErrors()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "a",
            email = "invalid",
            password = "weak",
            fullName = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.EnumerateObject().Any());
        Assert.DoesNotContain("weak", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RoleManagementPermissionAloneDoesNotGrantAdministratorAuthority()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);
        Guid roleId;
        Guid permissionId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = "delegate", Email = "delegate@daw.local", FullName = "Delegate" };
            Assert.True((await manager.CreateAsync(user, "User123!Test")).Succeeded);
            var permission = await db.Permissions.SingleAsync(item => item.Name == "roles.manage");
            db.UserPermissions.Add(new UserPermission { UserId = user.Id, PermissionId = permission.Id });
            await db.SaveChangesAsync();
            roleId = (await db.Roles.SingleAsync(role => role.Name == PermissionCatalog.ReadOnlyRole)).Id;
            permissionId = (await db.Permissions.SingleAsync(item => item.Name == "photos.create")).Id;
        }
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "delegate", "User123!Test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.PostAsync($"/api/admin/roles/{roleId}/permissions/{permissionId}", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var verification = factory.Services.CreateScope();
        Assert.False(await verification.ServiceProvider.GetRequiredService<AppDbContext>().RolePermissions.AnyAsync(
            link => link.RoleId == roleId && link.PermissionId == permissionId));
    }

    [Fact]
    public async Task AdminCanUploadListAndDeleteAnimalPhotos()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);

        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "admin", AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var animalId = await GetFirstAnimalIdAsync(client);

        var photoUrl = await UploadPhotoAsync(client, animalId, "photo.png");
        Assert.StartsWith($"/api/animals/{animalId}/photos/", photoUrl);
        Assert.EndsWith("/content", photoUrl);

        using var served = await client.GetAsync(photoUrl);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal("image/png", served.Content.Headers.ContentType?.MediaType);
        Assert.Equal(TestImages.Png, await served.Content.ReadAsByteArrayAsync());
        Assert.Equal("nosniff", Assert.Single(served.Headers.GetValues("X-Content-Type-Options")));
        using var anonymous = factory.CreateClient();
        using var denied = await anonymous.GetAsync(photoUrl);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        using var detail = await client.GetAsync($"/api/animals/{animalId}");
        using var animal = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        var photos = animal.RootElement.GetProperty("photos");
        Assert.Equal(1, photos.GetArrayLength());
        Assert.Equal(photoUrl, photos[0].GetProperty("url").GetString());
        var photoId = photos[0].GetProperty("id").GetGuid();

        using var list = await client.GetAsync("/api/animals");
        using var animals = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var listed = animals.RootElement.EnumerateArray().First(item => item.GetProperty("id").GetGuid() == animalId);
        Assert.Equal(photoUrl, listed.GetProperty("coverPhotoUrl").GetString());
        Assert.Equal(1, listed.GetProperty("photoCount").GetInt32());

        using var delete = await client.DeleteAsync($"/api/animals/{animalId}/photos/{photoId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        using var afterDelete = await client.GetAsync($"/api/animals/{animalId}");
        using var afterAnimal = JsonDocument.Parse(await afterDelete.Content.ReadAsStringAsync());
        Assert.Equal(0, afterAnimal.RootElement.GetProperty("photos").GetArrayLength());
        using var removed = await client.GetAsync(photoUrl);
        Assert.Equal(HttpStatusCode.NotFound, removed.StatusCode);
    }

    [Fact]
    public async Task StaleEndpointListsAnimalsNotUpdatedRecently()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var farm = await db.Farms.FirstAsync();
            var species = await db.Species.FirstAsync();

            db.Animals.Add(new Animal
            {
                FarmId = farm.Id,
                SpeciesId = species.Id,
                InternalTag = "OLD-1",
                Sex = Sex.Male,
                Status = AnimalStatus.Active,
                Origin = AnimalOrigin.Born,
                Purpose = ProductivePurpose.Meat,
                HealthStatus = HealthStatus.Healthy,
                UpdatedAt = DateTime.UtcNow.AddDays(-90)
            });

            await db.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "admin", AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/api/animals/stale?days=60");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var stale = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var tags = stale.RootElement.EnumerateArray().Select(item => item.GetProperty("internalTag").GetString()).ToList();
        Assert.Contains("OLD-1", tags);
        Assert.DoesNotContain("DEMO-001", tags);
    }

    private static async Task<string> UploadPhotoAsync(HttpClient client, Guid animalId, string fileName)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(TestImages.Png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", fileName);

        using var upload = await client.PostAsync($"/api/animals/{animalId}/photo", form);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        using var uploaded = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        return uploaded.RootElement.GetProperty("url").GetString()!;
    }

    private static async Task<Guid> GetFirstAnimalIdAsync(HttpClient client)
    {
        using var list = await client.GetAsync("/api/animals");
        list.EnsureSuccessStatusCode();
        using var animals = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        return animals.RootElement[0].GetProperty("id").GetGuid();
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
