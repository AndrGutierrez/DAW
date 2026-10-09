using System.Security.Claims;
using Core.Application.Security;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security;

public sealed class FarmAccess(AppDbContext db, IHttpContextAccessor httpContextAccessor) : IFarmAccess
{
    public async Task<bool> CanAccessAsync(Guid farmId, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null || farmId == Guid.Empty)
        {
            return false;
        }

        if (await HasAllFarmAccessAsync(user, cancellationToken))
        {
            return await db.Farms.AsNoTracking().AnyAsync(farm => farm.Id == farmId, cancellationToken);
        }

        return await db.UserFarms.AsNoTracking()
            .AnyAsync(membership => membership.UserId == user.Id && membership.FarmId == farmId && db.Farms.Any(f => f.Id == membership.FarmId),
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetAccessibleFarmIdsAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Array.Empty<Guid>();
        }

        if (await HasAllFarmAccessAsync(user, cancellationToken))
        {
            return await db.Farms.AsNoTracking().Select(farm => farm.Id).ToListAsync(cancellationToken);
        }

        return await db.UserFarms.AsNoTracking()
            .Where(membership => membership.UserId == user.Id && db.Farms.Any(f => f.Id == membership.FarmId))
            .Select(membership => membership.FarmId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true
            || !Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return null;
        }

        return await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == userId && user.IsActive, cancellationToken);
    }

    private async Task<bool> HasAllFarmAccessAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        user.IsSuperuser || await db.UserRoles.AsNoTracking().AnyAsync(
            membership => membership.UserId == user.Id && db.Roles.Any(role => role.Id == membership.RoleId
                && (role.Name == PermissionCatalog.AdminRole || role.Name == PermissionCatalog.AdministratorRole)),
            cancellationToken);
}
