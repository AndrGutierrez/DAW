using System.ComponentModel.DataAnnotations;
using Core.Application.Livestock;
namespace Core.Application.Management;
public sealed record ArchiveEntry
{
    public Guid Id { get; init; }
    public string Resource { get; init; } = "";
    public string Label { get; init; } = "";
    public Guid? FarmId { get; init; }
    public DateTime DeletedAt { get; init; }
    public Guid? DeletedByUserId { get; init; }
}
public sealed record ArchiveDetail(ArchiveEntry Entry, string Values);
public sealed class ArchiveQuery
{
    [Range(1, 1000000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 25;
    [StringLength(100)] public string? Search { get; init; }
    [StringLength(30)] public string? Resource { get; init; }
}
public interface IArchiveStore
{
    Task<CarePage<ArchiveEntry>> PageAsync(ArchiveQuery query, CancellationToken ct);
    Task<ArchiveDetail> DetailAsync(string resource, Guid id, CancellationToken ct);
    Task RestoreAsync(string resource, Guid id, CancellationToken ct);
}
