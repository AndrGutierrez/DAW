using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Application.Cattle;
using Infrastructure.Cattle;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Tests;

public sealed class ApiIntegrationTests
{
    [Theory]
    [InlineData("not-found", HttpStatusCode.NotFound, "Not Found")]
    [InlineData("invalid-operation", HttpStatusCode.BadRequest, "Bad Request")]
    [InlineData("unexpected", HttpStatusCode.InternalServerError, "Internal Server Error")]
    public async Task RegisteredPipelineReturnsProblemDetails(
        string kind,
        HttpStatusCode expectedStatus,
        string expectedTitle)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var path = $"/api/demo/errors/{kind}";

        using var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();
        using var problem = JsonDocument.Parse(body);

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("about:blank", problem.RootElement.GetProperty("type").GetString());
        Assert.Equal(expectedTitle, problem.RootElement.GetProperty("title").GetString());
        Assert.Equal((int)expectedStatus, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(path, problem.RootElement.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("detail").GetString()));
        Assert.DoesNotContain("stackTrace", body);

        if (expectedStatus == HttpStatusCode.InternalServerError)
        {
            Assert.Equal("An unexpected error occurred.", problem.RootElement.GetProperty("detail").GetString());
            Assert.DoesNotContain("Simulated internal detail", body);
        }
    }

    [Fact]
    public async Task HerdsEndpointsRetainRecordsAcrossRequests()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var herdResponse = await client.PostAsJsonAsync("/api/herds", new
        {
            name = "North Pasture",
            description = "Breeding herd"
        });
        Assert.Equal(HttpStatusCode.Created, herdResponse.StatusCode);
        using var herd = JsonDocument.Parse(await herdResponse.Content.ReadAsStringAsync());
        var herdId = herd.RootElement.GetProperty("id").GetGuid();

        using var getResponse = await client.GetAsync($"/api/herds/{herdId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        using var retrieved = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal(herdId, retrieved.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("North Pasture", retrieved.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public void DependencyInjectionUsesExpectedLifetimes()
    {
        using var factory = CreateFactory();
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();

        var first = firstScope.ServiceProvider;
        var second = secondScope.ServiceProvider;

        Assert.Same(first.GetRequiredService<IAnimalTagNormalizer>(), second.GetRequiredService<IAnimalTagNormalizer>());
        Assert.NotSame(first.GetRequiredService<IAnimalRegistrationValidator>(), first.GetRequiredService<IAnimalRegistrationValidator>());
        Assert.Same(first.GetRequiredService<ICattleRepository>(), first.GetRequiredService<ICattleRepository>());
        Assert.NotSame(first.GetRequiredService<ICattleRepository>(), second.GetRequiredService<ICattleRepository>());
        Assert.Same(first.GetRequiredService<ICattleCatalogService>(), first.GetRequiredService<ICattleCatalogService>());
        Assert.NotSame(first.GetRequiredService<ICattleCatalogService>(), second.GetRequiredService<ICattleCatalogService>());
        Assert.Same(first.GetRequiredService<InMemoryCattleStore>(), second.GetRequiredService<InMemoryCattleStore>());
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DisableHttpsRedirection"] = "true",
                    ["Jwt:Key"] = new string('t', 64)
                }));
        });
}
