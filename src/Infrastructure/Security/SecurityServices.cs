using Core.Application.Security;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security;

public sealed class PermissionChecker(AppDbContext db) : IPermissionChecker
{
    public async Task<bool> HasPermissionAsync(
        Guid userId,
        string permissionName,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return false;
        }

        if (user.IsSuperuser)
        {
            return true;
        }

        var hasDirectPermission = await db.UserPermissions
            .AsNoTracking()
            .AnyAsync(
                userPermission => userPermission.UserId == userId
                    && userPermission.Permission.Name == permissionName,
                cancellationToken);

        if (hasDirectPermission)
        {
            return true;
        }

        return await db.RolePermissions
            .AsNoTracking()
            .AnyAsync(
                rolePermission => rolePermission.Permission.Name == permissionName
                    && db.UserRoles.Any(
                        userRole => userRole.UserId == userId
                            && userRole.RoleId == rolePermission.RoleId),
                cancellationToken);
    }
}
