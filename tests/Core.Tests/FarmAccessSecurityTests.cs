using System.Security.Claims;
using Core.Application.Storage;
using Core.Domain.Livestock;
using Infrastructure.Livestock;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Identity;
using Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests;

public sealed class FarmAccessSecurityTests
{
    [Fact]
    public async Task MembershipScopesListDetailAndStaleReadsToTheAssignedFarm()
    {
        await using var db = CreateContext();
        var (user, allowed, other) = await SeedAsync(db);
        var access = CreateAccess(db, user.Id);
        var queries = new AnimalQueryService(db, new AnimalWeightReader(db), access);

        Assert.True(await access.CanAccessAsync(allowed.FarmId));
        Assert.False(await access.CanAccessAsync(other.FarmId));
        Assert.Equal(allowed.Id, Assert.Single(await queries.ListAsync()).Id);
        Assert.Equal(allowed.Id, Assert.Single(await queries.ListStaleAsync(30)).Id);
        Assert.Equal(allowed.Id, (await queries.GetAsync(allowed.Id)).Id);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => queries.GetAsync(other.Id));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Administrador")]
    public async Task DatabaseAdministratorRolesCanAccessAllExistingFarms(string roleName)
    {
        await using var db = CreateContext();
        var (user, allowed, other) = await SeedAsync(db);
        var role = new Role { Id = Guid.NewGuid(), Name = roleName, NormalizedName = roleName.ToUpperInvariant() };
        db.Roles.Add(role);
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();
        var access = CreateAccess(db, user.Id);

        Assert.True(await access.CanAccessAsync(other.FarmId));
        Assert.Equal(2, (await access.GetAccessibleFarmIdsAsync()).Count);
        Assert.False(await access.CanAccessAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task StaleAdministratorClaimCannotGrantAccessWithoutTheDatabaseRole()
    {
        await using var db = CreateContext();
        var (user, allowed, other) = await SeedAsync(db);
        var access = CreateAccess(db, user.Id, new Claim(ClaimTypes.Role, "Admin"));

        Assert.True(await access.CanAccessAsync(allowed.FarmId));
        Assert.False(await access.CanAccessAsync(other.FarmId));
        Assert.Single(await access.GetAccessibleFarmIdsAsync());
    }

    [Fact]
    public async Task SuperuserAccessEndsImmediatelyWhenTheAccountIsDeactivated()
    {
        await using var db = CreateContext();
        var (user, _, other) = await SeedAsync(db);
        user.IsSuperuser = true;
        await db.SaveChangesAsync();
        var access = CreateAccess(db, user.Id);
        Assert.True(await access.CanAccessAsync(other.FarmId));

        user.IsActive = false;
        await db.SaveChangesAsync();

        Assert.False(await access.CanAccessAsync(other.FarmId));
        Assert.Empty(await access.GetAccessibleFarmIdsAsync());
    }

    [Fact]
    public async Task AnonymousAndUnassignedUsersHaveNoFarmAccess()
    {
        await using var db = CreateContext();
        var (user, allowed, _) = await SeedAsync(db);
        db.UserFarms.RemoveRange(db.UserFarms);
        await db.SaveChangesAsync();

        Assert.Empty(await CreateAccess(db, user.Id).GetAccessibleFarmIdsAsync());
        var anonymous = new FarmAccess(db, new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        Assert.False(await anonymous.CanAccessAsync(allowed.FarmId));
        Assert.Empty(await anonymous.GetAccessibleFarmIdsAsync());
    }

    [Fact]
    public async Task CrossFarmPhotoRequestsCannotTouchStorageOrMetadata()
    {
        await using var db = CreateContext();
        var (user, _, other) = await SeedAsync(db);
        var photo = new AnimalPhoto
        {
            FarmId = other.FarmId,
            AnimalId = other.Id,
            Url = "/uploads/animals/photo.png",
            FileName = "photo.png"
        };
        db.AnimalPhotos.Add(photo);
        await db.SaveChangesAsync();
        var storage = new RecordingStorage();
        var service = new AnimalPhotoService(db, storage, CreateAccess(db, user.Id));
        using var content = new MemoryStream(TestImages.Png);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UploadAsync(
            other.Id, content, "photo.png", "image/png", TestImages.Png.Length, user.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAsync(other.Id, photo.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.OpenReadAsync(other.Id, photo.Id));

        Assert.Equal(0, storage.Saves);
        Assert.Equal(0, storage.Deletes);
        Assert.Equal(0, storage.Reads);
        Assert.True(await db.AnimalPhotos.AnyAsync(candidate => candidate.Id == photo.Id));
    }

    private static AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase("farm-security-" + Guid.NewGuid()).Options);

    private static FarmAccess CreateAccess(AppDbContext db, Guid userId, params Claim[] claims) =>
        new(db, new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }.Concat(claims), "test"))
            }
        });

    private static async Task<(ApplicationUser User, Animal Allowed, Animal Other)> SeedAsync(AppDbContext db)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "operator", FullName = "Operator" };
        var species = new Species { Name = "Bovino", Code = "BO", Purpose = ProductivePurpose.Meat };
        Animal NewAnimal(string tag) => new()
        {
            Farm = new Farm { Name = "Farm " + tag, Code = tag },
            Species = species,
            InternalTag = tag,
            Sex = Sex.Male,
            Purpose = ProductivePurpose.Meat,
            UpdatedAt = DateTime.UtcNow.AddDays(-90)
        };
        var allowed = NewAnimal("A1");
        var other = NewAnimal("B1");
        db.Users.Add(user);
        db.Animals.AddRange(allowed, other);
        db.UserFarms.Add(new UserFarm { UserId = user.Id, FarmId = allowed.Farm.Id });
        await db.SaveChangesAsync();
        return (user, allowed, other);
    }

    private sealed class RecordingStorage : IFileStorage
    {
        public int Saves { get; private set; }
        public int Deletes { get; private set; }
        public int Reads { get; private set; }
        public Task<StoredFile> SaveAsync(Stream content, string folder, string fileName, string contentType,
            CancellationToken cancellationToken = default)
        {
            Saves++;
            return Task.FromResult(new StoredFile("/uploads/photo.png", "photo.png", TestImages.Png.Length));
        }
        public Task DeleteAsync(string relativeUrl, CancellationToken cancellationToken = default)
        {
            Deletes++;
            return Task.CompletedTask;
        }
        public Task DeleteAsync(string folder, string storedFileName, CancellationToken cancellationToken = default) =>
            DeleteAsync(folder + "/" + storedFileName, cancellationToken);
        public Task<Stream> OpenReadAsync(string folder, string storedFileName, CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult<Stream>(new MemoryStream(TestImages.Png));
        }
    }
}
