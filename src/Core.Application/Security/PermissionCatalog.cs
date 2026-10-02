namespace Core.Application.Security;

public static class PermissionCatalog
{
    public const string GuardName = "web";

    public static readonly string[] Actions = ["list", "get"];

    public static readonly string[] Resources =
    [
        "animals",
        "farms",
        "lots",
        "paddocks",
        "species",
        "breeds",
        "users",
        "roles",
        "permissions"
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
    }

    public static string Name(string resource, string action) => $"{resource}.{action}";
}

public sealed record PermissionDefinition(string Name, string Resource, string Action, string GuardName);
