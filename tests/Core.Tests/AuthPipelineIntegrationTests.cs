using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Core.Tests;

public sealed class AuthPipelineIntegrationTests
{
    [Theory]
    [InlineData("/api/auth/me")]
    [InlineData("/api/admin/roles")]
    [InlineData("/api/admin/permissions")]
    public async Task ProtectedEndpointsRejectAnonymousRequests(string path)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerDocumentExposesBearerSecuritySchemeAndAuthPaths()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        var securitySchemes = root.GetProperty("components").GetProperty("securitySchemes");
        Assert.True(securitySchemes.TryGetProperty("Bearer", out var bearer));
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());

        var paths = root.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/auth/login", out _));
        Assert.True(paths.TryGetProperty("/api/animals/{id}/photo", out _));
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
