using Core.Domain.Assets;

namespace Core.Application.Assets;

public sealed record CategoryResult(Guid Id, string Name, string? Description, DateTime CreatedAt);

public sealed record AssetResult(
    Guid Id,
    string AssetTag,
    string Name,
    string? SerialNumber,
    AssetStatus Status,
    Guid CategoryId,
    string CategoryName,
    DateTime CreatedAt);
