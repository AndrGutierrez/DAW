namespace Core.Application.Security;

public sealed record PermissionResult(Guid Id, string Name, string GuardName);

public sealed record RoleResult(
    Guid Id,
    string Name,
    string GuardName,
    string? Description,
    IReadOnlyList<string> Permissions);

public interface IAccessAdministration
{
    Task<IReadOnlyList<PermissionResult>> ListPermissionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleResult>> ListRolesAsync(CancellationToken cancellationToken = default);

    Task<RoleResult> GrantPermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);

    Task<RoleResult> RevokePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
}
