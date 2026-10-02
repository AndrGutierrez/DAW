using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Core.Application.Security;
using Infrastructure.Security;
using Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Core.Tests;

public sealed class PermissionCatalogTests
{
    [Fact]
    public void CatalogDefinesUniqueWellFormedPermissions()
    {
        var definitions = PermissionCatalog.All().ToList();

        Assert.Equal(9 * 2, definitions.Count);
        Assert.Equal(definitions.Count, definitions.Select(definition => definition.Name).Distinct().Count());

        foreach (var definition in definitions)
        {
            Assert.Matches("^[a-z]+\\.[a-z]+$", definition.Name);
            Assert.Equal(PermissionCatalog.GuardName, definition.GuardName);
            Assert.Equal($"{definition.Resource}.{definition.Action}", definition.Name);
        }

        Assert.Contains(definitions, definition => definition.Name == "animals.list");
        Assert.Contains(definitions, definition => definition.Name == "animals.get");
        Assert.Contains(definitions, definition => definition.Name == "permissions.list");
    }
}

public sealed class JwtOptionsValidatorTests
{
    private readonly JwtOptionsValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("change-me-to-a-strong-random-secret-at-least-32-chars")]
    [InlineData("short")]
    public void InvalidKeysAreRejected(string key)
    {
        var result = _validator.Validate(null, new JwtOptions { Key = key });

        Assert.True(result.Failed);
    }

    [Fact]
    public void ValidKeyIsAccepted()
    {
        var result = _validator.Validate(null, new JwtOptions { Key = new string('k', 64) });

        Assert.Same(ValidateOptionsResult.Success, result);
    }
}

public sealed class JwtTokenServiceTests
{
    private static JwtTokenService CreateService() =>
        new(Options.Create(new JwtOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            Key = new string('k', 64),
            AccessTokenMinutes = 5
        }));

    [Fact]
    public void AccessTokenCarriesIdentityRolesAndSuperuserClaims()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var (token, expiresAt) = service.CreateAccessToken(userId, "jane", ["Administrador"], isSuperuser: true);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("issuer", parsed.Issuer);
        Assert.Contains(parsed.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == userId.ToString());
        Assert.Contains(parsed.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Administrador");
        Assert.Contains(parsed.Claims, claim => claim.Type == "superuser" && claim.Value == "true");
        Assert.True(expiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void RefreshTokensAreRandomAndLongEnough()
    {
        var service = CreateService();

        var first = service.CreateRefreshToken();
        var second = service.CreateRefreshToken();

        Assert.NotEqual(first, second);
        Assert.True(first.Length >= 32);
    }
}

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "daw-storage-tests", Guid.NewGuid().ToString("N"));

    private LocalFileStorage CreateStorage(long maxBytes = 1024) =>
        new(Options.Create(new StorageOptions
        {
            RootPath = _root,
            RequestPath = "/uploads",
            MaxFileSizeBytes = maxBytes,
            AllowedContentTypes = ["image/png"]
        }));

    [Fact]
    public async Task SavedFileIsWrittenAndServedUnderRequestPath()
    {
        var storage = CreateStorage();

        using var content = new MemoryStream([1, 2, 3]);
        var stored = await storage.SaveAsync(content, "animals/abc", "photo.png", "image/png");

        Assert.StartsWith("/uploads/animals/abc/", stored.RelativeUrl);
        Assert.EndsWith(".png", stored.StoredFileName);
        Assert.Equal(3, stored.SizeBytes);
        Assert.True(File.Exists(Path.Combine(_root, "animals", "abc", stored.StoredFileName)));

        await storage.DeleteAsync(stored.RelativeUrl);

        Assert.False(File.Exists(Path.Combine(_root, "animals", "abc", stored.StoredFileName)));
    }

    [Fact]
    public async Task DisallowedContentTypeIsRejected()
    {
        var storage = CreateStorage();
        using var content = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.SaveAsync(content, "animals/abc", "notes.txt", "text/plain"));
    }

    [Fact]
    public async Task OversizedFileIsRejectedAndNotKept()
    {
        var storage = CreateStorage(maxBytes: 2);
        using var content = new MemoryStream([1, 2, 3, 4]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.SaveAsync(content, "animals/abc", "photo.png", "image/png"));

        Assert.False(Directory.Exists(Path.Combine(_root, "animals", "abc"))
            && Directory.EnumerateFiles(Path.Combine(_root, "animals", "abc")).Any());
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
