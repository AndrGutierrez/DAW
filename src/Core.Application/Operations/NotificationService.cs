using Core.Application.Security;
namespace Core.Application.Operations;

public enum NotificationSeverity { Information, Warning, Critical }
public sealed record OperationalNotification(string Id, Guid FarmId, string Kind, NotificationSeverity Severity,
    string Title, string Message, string Href);
public sealed record NotificationVisibility(bool Inventory, bool Health, bool Paddocks, bool Growth);
public sealed record NotificationSnapshot(IReadOnlyList<OperationalNotification> Items, int Total, DateTime GeneratedAt);
public interface INotificationReader
{
    Task<NotificationSnapshot> ReadAsync(IReadOnlyCollection<Guid> farmIds, NotificationVisibility visibility, CancellationToken ct);
}

public sealed class NotificationService(ICurrentUser user, IPermissionChecker permissions, IFarmAccess farms, INotificationReader reader)
{
    public async Task<NotificationSnapshot> GetAsync(CancellationToken ct = default)
    {
        var id = user.UserId ?? throw new UnauthorizedAccessException();
        var allowed = new Dictionary<string, bool>();
        foreach (var permission in new[] { "inventory.list", "products.list", "animals.list", "animals.get", "clinical.list", "paddocks.list", "weights.list" })
            allowed[permission] = await permissions.HasPermissionAsync(id, permission, ct);
        var animalAccess = allowed["animals.list"] && allowed["animals.get"];
        var visibility = new NotificationVisibility(allowed["inventory.list"] && allowed["products.list"],
            animalAccess && allowed["clinical.list"], allowed["paddocks.list"], animalAccess && allowed["weights.list"]);
        return await reader.ReadAsync(await farms.GetAccessibleFarmIdsAsync(ct), visibility, ct);
    }
}
