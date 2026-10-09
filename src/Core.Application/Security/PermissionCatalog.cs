namespace Core.Application.Security;

public static class PermissionCatalog
{
    public const string GuardName = "web";

    public const string AdminRole = "Admin";
    public const string EmployeeRole = "Employee";
    public const string AdministratorRole = "Administrador";
    public const string ReadOnlyRole = "SoloLectura";
    public const string AdminRoles = AdminRole + "," + AdministratorRole;

    public static readonly string[] Actions = ["list", "get", "create", "update", "delete"];

    public static readonly string[] Resources =
    [
        "animals",
        "farms",
        "lots",
        "paddocks",
        "species",
        "breeds",
        "categories",
        "products",
        "inventory",
        "production",
        "weights",
        "users",
        "roles",
        "permissions",
        "photos"
    ];

    public static IEnumerable<PermissionDefinition> All()
    {
        foreach (var resource in Resources)
        {
            foreach (var action in Actions)
            {
                yield return new PermissionDefinition($"{resource}.{action}", resource, action, GuardName);
            }
        }

        foreach (var resource in new[] { "clinical", "reproduction" })
            foreach (var action in new[] { "list", "create" })
                yield return new PermissionDefinition($"{resource}.{action}", resource, action, GuardName);
        foreach (var action in new[] { "list", "get" })
            yield return new PermissionDefinition($"auditlogs.{action}", "auditlogs", action, GuardName);
        foreach (var action in new[] { "list", "get", "restore" })
            yield return new PermissionDefinition($"archive.{action}", "archive", action, GuardName);
        yield return new PermissionDefinition("roles.manage", "roles", "manage", GuardName);
    }

    public static string Name(string resource, string action) => $"{resource}.{action}";
}

public sealed record PermissionDefinition(string Name, string Resource, string Action, string GuardName);
