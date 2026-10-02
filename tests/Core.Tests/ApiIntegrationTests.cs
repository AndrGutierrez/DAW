using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Application.Management;
using Core.Application.Security;
using FluentValidation;
using Infrastructure.Persistence;
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
    public async Task HerdsCompatibilityRouteRequiresAuthentication()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var herdResponse = await client.PostAsJsonAsync("/api/herds", new
        {
            name = "North Pasture",
            description = "Breeding herd"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, herdResponse.StatusCode);
        Assert.Equal("application/problem+json", herdResponse.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await herdResponse.Content.ReadAsStringAsync());
        foreach (var field in new[] { "type", "title", "status", "detail", "instance" }) Assert.True(problem.RootElement.TryGetProperty(field, out _));
    }

    [Fact]
    public void DependencyInjectionUsesExpectedLifetimes()
    {
        using var factory = CreateFactory();
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();

        var first = firstScope.ServiceProvider;
        var second = secondScope.ServiceProvider;

        Assert.Same(first.GetRequiredService<ITokenService>(), second.GetRequiredService<ITokenService>());
        Assert.NotSame(first.GetRequiredService<IValidator<AnimalRequest>>(), first.GetRequiredService<IValidator<AnimalRequest>>());
        Assert.Same(first.GetRequiredService<AppDbContext>(), first.GetRequiredService<AppDbContext>());
        Assert.NotSame(first.GetRequiredService<AppDbContext>(), second.GetRequiredService<AppDbContext>());
        Assert.Same(first.GetRequiredService<ICrudService<AnimalRequest>>(), first.GetRequiredService<ICrudService<AnimalRequest>>());
        Assert.NotSame(first.GetRequiredService<ICrudService<AnimalRequest>>(), second.GetRequiredService<ICrudService<AnimalRequest>>());
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
