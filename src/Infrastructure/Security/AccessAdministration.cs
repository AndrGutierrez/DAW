using Core.Application.Security;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security;

public sealed class AccessAdministration(AppDbContext db) : IAccessAdministration
{
    public async Task<IReadOnlyList<PermissionResult>> ListPermissionsAsync(CancellationToken cancellationToken = default) =>
        await db.Permissions
            .AsNoTracking()
            .OrderBy(permission => permission.Name)
            .Select(permission => new PermissionResult(
                permission.Id,
                permission.Name,
                permission.GuardName))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RoleResult>> ListRolesAsync(CancellationToken cancellationToken = default) =>
        await db.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new RoleResult(
                role.Id,
                role.Name!,
                role.GuardName,
                role.Description,
                role.Permissions.Select(rolePermission => rolePermission.Permission.Name).ToList()))
            .ToListAsync(cancellationToken);

    public Task<RoleResult> GrantPermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default) =>
        ChangePermissionAsync(roleId, permissionId, grant: true, cancellationToken);

    public Task<RoleResult> RevokePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default) =>
        ChangePermissionAsync(roleId, permissionId, grant: false, cancellationToken);

    private async Task<RoleResult> ChangePermissionAsync(
        Guid roleId,
        Guid permissionId,
        bool grant,
        CancellationToken cancellationToken)
    {
        if (roleId == Guid.Empty || permissionId == Guid.Empty)
        {
            throw new ArgumentException("Role and permission identifiers must not be empty.");
        }

        var role = await db.Roles
            .FirstOrDefaultAsync(candidate => candidate.Id == roleId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested role was not found.");

        var permission = await db.Permissions
            .FirstOrDefaultAsync(candidate => candidate.Id == permissionId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested permission was not found.");

        var link = await db.RolePermissions
            .FirstOrDefaultAsync(
                rolePermission => rolePermission.RoleId == roleId
                    && rolePermission.PermissionId == permissionId,
                cancellationToken);

        if (grant && link is null)
        {
            db.RolePermissions.Add(new Persistence.Identity.RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });

            await db.SaveChangesAsync(cancellationToken);
        }
        else if (!grant && link is not null)
        {
            db.RolePermissions.Remove(link);
            await db.SaveChangesAsync(cancellationToken);
        }

        var permissions = await db.RolePermissions
            .AsNoTracking()
            .Where(rolePermission => rolePermission.RoleId == roleId)
            .Select(rolePermission => rolePermission.Permission.Name)
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        return new RoleResult(role.Id, role.Name!, role.GuardName, role.Description, permissions);
    }
}
