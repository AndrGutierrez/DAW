using System.ComponentModel.DataAnnotations;
using Core.Application.Livestock;

namespace Core.Application.Auditing;

public sealed class AuditQuery
{
    [Range(1, 1000000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 25;
    [MaxLength(150)] public string? Search { get; set; }
    [MaxLength(100)] public string? Actor { get; set; }
    [MaxLength(50)] public string? Action { get; set; }
    [MaxLength(150)] public string? EntityName { get; set; }
    public Guid? UserId { get; set; }
    public Guid? FarmId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public sealed record AuditEntry
{
    public Guid Id { get; init; }
    public DateTime OccurredAt { get; init; }
    public string Action { get; init; } = "";
    public string EntityName { get; init; } = "";
    public string? EntityId { get; init; }
    public Guid? UserId { get; init; }
    public string? ActorName { get; init; }
    public Guid? FarmId { get; init; }
    public string? FarmName { get; init; }
}
public sealed record AuditDetail(AuditEntry Entry, string? IpAddress, string? OldValues, string? NewValues);
public sealed record AuditFarm(Guid Id, string Name);
public sealed record AuditOptions(IReadOnlyList<string> Actions, IReadOnlyList<string> Entities, IReadOnlyList<AuditFarm> Farms);

// Read-only persistence port. Audit history has no update or delete operation.
public interface IAuditReader
{
    Task<CarePage<AuditEntry>> PageAsync(AuditQuery query, CancellationToken ct = default);
    Task<AuditDetail> GetAsync(Guid id, CancellationToken ct = default);
    Task<AuditOptions> OptionsAsync(CancellationToken ct = default);
}
