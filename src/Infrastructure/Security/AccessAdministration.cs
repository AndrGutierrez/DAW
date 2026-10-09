using Core.Application.Security;
using Core.Domain.Livestock;
using System.Text.Json;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security;

public sealed class AccessAdministration(AppDbContext db, ICurrentUser actor) : IAccessAdministration
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

        var link = await db.RolePermissions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                rolePermission => rolePermission.RoleId == roleId
                    && rolePermission.PermissionId == permissionId,
                cancellationToken);

        if (grant && (link is null || db.Entry(link).Property<bool>("IsDeleted").CurrentValue))
        {
            if (link is not null) db.RestoreLink(link);
            else db.RolePermissions.Add(new Persistence.Identity.RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });

            RecordAudit(roleId, role.Name!, permissionId, permission.Name, grant);
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (!grant && link is not null && !db.Entry(link).Property<bool>("IsDeleted").CurrentValue)
        {
            db.RolePermissions.Remove(link);
            RecordAudit(roleId, role.Name!, permissionId, permission.Name, grant);
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
    private void RecordAudit(Guid roleId, string roleName, Guid permissionId, string permissionName, bool grant) =>
        db.AuditLogs.Add(new AuditLog
        {
            UserId = actor.UserId, IpAddress = actor.IpAddress, EntityName = "RolePermission", EntityId = roleId.ToString(),
            Action = grant ? "RolePermissionGranted" : "RolePermissionRevoked",
            OldValues = JsonSerializer.Serialize(new { Role = roleName, Permission = permissionName, PermissionId = permissionId, Assigned = !grant }),
            NewValues = JsonSerializer.Serialize(new { Role = roleName, Permission = permissionName, PermissionId = permissionId, Assigned = grant })
        });

}
