using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var roles = await client.GetAsync("/api/admin/roles");
        Assert.Equal(HttpStatusCode.OK, roles.StatusCode);
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
        Assert.StartsWith("/uploads/", photoUrl);

        using var served = await client.GetAsync(photoUrl);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);

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
        var file = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
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
