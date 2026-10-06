using System.Net;
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

public sealed class BrowserSessionIntegrationTests
{
    [Fact]
    public async Task LoginReturnsAccessTokenAndPersistentHttpOnlyCookieWithoutRefreshTokenInJson()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);
        using var client = factory.CreateClient();
        await SetCsrfAsync(client);
        using var login = await LoginAsync(client);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.TryGetProperty("refreshToken", out _));
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("accessToken").GetString()));
        var cookie = RefreshCookie(login);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth/session", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("max-age=604800", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-store", login.Headers.CacheControl!.ToString());
        var raw = CookieValue(cookie);
        using var scope = factory.Services.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<AppDbContext>().RefreshTokens.SingleAsync();
        Assert.NotEqual(raw, stored.Token);
        Assert.StartsWith("sha256:", stored.Token);
    }

    [Fact]
    public async Task CookieRestoresSessionAndRotationRejectsReplay()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);
        using var client = factory.CreateClient();
        await SetCsrfAsync(client);
        using var login = await LoginAsync(client);
        var original = CookieValue(RefreshCookie(login));
        using var refreshed = await client.PostAsync("/api/auth/session/refresh", null);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.NotEqual(original, CookieValue(RefreshCookie(refreshed)));
        using var replay = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(original));
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task LogoutRevokesRefreshTokenAndClearsCookie()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);
        using var client = factory.CreateClient();
        await SetCsrfAsync(client);
        using var login = await LoginAsync(client);
        var original = CookieValue(RefreshCookie(login));
        using var logout = await client.PostAsync("/api/auth/session/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains("expires=Thu, 01 Jan 1970", RefreshCookie(logout));
        using var replay = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(original));
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        using var restore = await client.PostAsync("/api/auth/session/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, restore.StatusCode);
    }

    [Theory]
    [InlineData("login")]
    [InlineData("refresh")]
    [InlineData("logout")]
    public async Task CookieOperationsRejectMissingCsrf(string action)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var response = action == "login"
            ? await LoginAsync(client)
            : await client.PostAsync("/api/auth/session/" + action, null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task TokenWithoutMatchingAntiforgeryCookieIsRejected()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SetCsrfAsync(client);
        var token = client.DefaultRequestHeaders.GetValues("X-CSRF-TOKEN").Single();
        using var other = factory.CreateClient();
        other.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
        using var response = await LoginAsync(other);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BrowserContractRequiresHttpsOutsideLoopbackDevelopment()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://farm.example")
        });
        using var response = await client.GetAsync("/api/auth/session/csrf");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task HttpsCookiesCarrySecureFlag()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        await SetCsrfAsync(client);
        using var login = await LoginAsync(client);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains("secure", RefreshCookie(login), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HashFromDatabaseCannotBeUsedAsABearerRefreshToken()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);
        using var client = factory.CreateClient();
        await SetCsrfAsync(client);
        using var login = await LoginAsync(client);
        using var scope = factory.Services.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<AppDbContext>().RefreshTokens.SingleAsync();
        using var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(stored.Token));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LegacyPlaintextTokenIsAcceptedOnceAndReplacedWithHash()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);
        string legacy;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(user => user.UserName == "admin");
            legacy = scope.ServiceProvider.GetRequiredService<ITokenService>().CreateRefreshToken();
            db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, Token = legacy, ExpiresAt = DateTime.UtcNow.AddDays(1) });
            await db.SaveChangesAsync();
        }
        using var client = factory.CreateClient();
        using var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(legacy));
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        using var verification = factory.Services.CreateScope();
        var tokens = await verification.ServiceProvider.GetRequiredService<AppDbContext>().RefreshTokens.ToListAsync();
        Assert.NotNull(tokens.Single(token => token.Token == legacy).RevokedAt);
        Assert.Single(tokens, token => token.Token.StartsWith("sha256:") && token.RevokedAt is null);
    }

    [Fact]
    public async Task AnimalPagesAreBoundedStableAndSearchable()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);
        using var client = factory.CreateClient();
        await SetCsrfAsync(client);
        using var login = await LoginAsync(client);
        using var session = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
            session.RootElement.GetProperty("accessToken").GetString());
        using var first = await client.GetAsync("/api/animals/page?page=1&pageSize=2");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        using var page = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        Assert.Equal(2, page.RootElement.GetProperty("items").GetArrayLength());
        Assert.True(page.RootElement.GetProperty("total").GetInt32() >= 6);
        using var second = await client.GetAsync("/api/animals/page?page=2&pageSize=2");
        using var next = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        var ids = page.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid());
        Assert.DoesNotContain(next.RootElement.GetProperty("items").EnumerateArray(), item => ids.Contains(item.GetProperty("id").GetGuid()));
        using var search = await client.GetAsync("/api/animals/page?search=demo-001");
        using var matches = JsonDocument.Parse(await search.Content.ReadAsStringAsync());
        Assert.Equal(1, matches.RootElement.GetProperty("total").GetInt32());
        Assert.Equal("DEMO-001", matches.RootElement.GetProperty("items")[0].GetProperty("internalTag").GetString());
        using var invalid = await client.GetAsync("/api/animals/page?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task AnonymousCannotReadAnimalPages()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/animals/page");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AnimalPageCountsAndFiltersCannotExposeAnUnassignedFarm()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory);
        Guid hiddenFarmId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var visibleAnimal = await db.Animals.FirstAsync();
            var manager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            var reader = new ApplicationUser { UserName = "field-reader", Email = "field-reader@daw.local", FullName = "Field reader" };
            Assert.True((await manager.CreateAsync(reader, "User123!Test")).Succeeded);
            Assert.True((await manager.AddToRoleAsync(reader, "Employee")).Succeeded);
            db.UserFarms.Add(new Core.Domain.Livestock.UserFarm { UserId = reader.Id, FarmId = visibleAnimal.FarmId });
            var hidden = new Core.Domain.Livestock.Farm { Name = "Private farm", Code = "PRIVATE-TEST" };
            hiddenFarmId = hidden.Id;
            db.Farms.Add(hidden);
            db.Animals.Add(new Core.Domain.Livestock.Animal
            {
                FarmId = hidden.Id, SpeciesId = visibleAnimal.SpeciesId, InternalTag = "PRIVATE-ANIMAL",
                Sex = Core.Domain.Livestock.Sex.Female, Purpose = Core.Domain.Livestock.ProductivePurpose.Milk
            });
            await db.SaveChangesAsync();
        }
        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("field-reader", "User123!Test"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var session = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
            session.RootElement.GetProperty("accessToken").GetString());
        foreach (var filter in new[] { "search=PRIVATE", "farmId=" + hiddenFarmId })
        {
            using var response = await client.GetAsync("/api/animals/page?" + filter);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var page = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(0, page.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(0, page.RootElement.GetProperty("items").GetArrayLength());
        }
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client) =>
        client.PostAsJsonAsync("/api/auth/session/login", new LoginRequest("admin", "Admin123!Test"));

    private static string RefreshCookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("daw.refresh="));

    private static string CookieValue(string header) => Uri.UnescapeDataString(header.Split(';')[0]["daw.refresh=".Length..]);

    private static async Task SetCsrfAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/auth/session/csrf");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", json.RootElement.GetProperty("requestToken").GetString());
    }

    private static async Task SeedAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = "browser-session-" + Guid.NewGuid();
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DisableHttpsRedirection"] = "true",
                ["Jwt:Key"] = new string('t', 64),
                ["Seed:AdminUsername"] = "admin",
                ["Seed:AdminEmail"] = "admin@daw.local",
                ["Seed:AdminPassword"] = "Admin123!Test",
                ["Storage:RootPath"] = Path.Combine(Path.GetTempPath(), "daw-session-tests", Guid.NewGuid().ToString("N"))
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        });
    }
}
