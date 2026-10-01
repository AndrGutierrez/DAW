namespace Core.Application.Security;

public static class PermissionCatalog
{
    public const string GuardName = "web";

    public static readonly string[] Actions = ["add", "change", "delete", "view"];

    public static readonly (string Module, string Entity)[] Resources =
    [
        ("animals", "animal"),
        ("herds", "herd"),
        ("farms", "farm"),
        ("users", "user"),
        ("roles", "role"),
        ("permissions", "permission")
    ];

    public static IEnumerable<PermissionDefinition> All()
    {
        foreach (var (module, entity) in Resources)
        {
            foreach (var action in Actions)
            {
                yield return new PermissionDefinition(
                    $"{module}.{action}_{entity}",
                    module,
                    action,
                    GuardName);
            }
        }
    }

    public static string Name(string module, string action, string entity) =>
        $"{module}.{action}_{entity}";
}

public sealed record PermissionDefinition(string Name, string Module, string Action, string GuardName);
