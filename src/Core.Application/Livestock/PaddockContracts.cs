using Core.Application.Management;
using FluentValidation;

namespace Core.Application.Livestock;

public sealed record PaddockPageRequest(int Page = 1, int PageSize = 12, Guid? FarmId = null, string? Search = null, bool? IsActive = true);
public sealed record PaddockLotCount(Guid? Id, string? Name, int Count);
public sealed record PaddockSnapshot(Guid Id, PaddockRequest Data, string Farm, int Occupancy, DateOnly? OldestKnownArrival, int UnknownArrivals, IReadOnlyList<PaddockLotCount> Lots);
public sealed record PaddockResidentPageRequest(int Page = 1, int PageSize = 10, Guid? LotId = null, bool Ungrouped = false);
public sealed record PaddockResident(Guid Id, string InternalTag, string? Name, string? Lot, Guid? LotId, Guid SpeciesId, DateOnly? ArrivalDate);
public interface IPaddockReader
{
    Task<IReadOnlyList<PaddockSnapshot>> MapAsync(Guid farmId, CancellationToken ct);
    Task<CarePage<PaddockSnapshot>> PageAsync(PaddockPageRequest request, IReadOnlyCollection<Guid> farmIds, CancellationToken ct);
    Task<CarePage<PaddockResident>> ResidentsAsync(Guid paddockId, PaddockResidentPageRequest query, CancellationToken ct);
}
public sealed record AnimalMovementRequest(Guid SubmissionId, Guid? ToPaddockId, Guid? ToLotId, Guid? ExpectedFromPaddockId, Guid? ExpectedFromLotId, string Reason);
public sealed record AnimalMovementRecord(Guid Id, Guid? FromPaddockId, Guid? ToPaddockId, Guid? FromLotId, Guid? ToLotId, DateOnly Date, string? Reason, Guid? UserId);
public sealed class PaddockPageRequestValidator : AbstractValidator<PaddockPageRequest>
{
    public PaddockPageRequestValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 100000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 48);
        RuleFor(x => x.FarmId).NotEqual(Guid.Empty).When(x => x.FarmId.HasValue);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
public sealed class AnimalMovementRequestValidator : AbstractValidator<AnimalMovementRequest>
{
    public AnimalMovementRequestValidator()
    {
        RuleFor(x => x.SubmissionId).NotEmpty();
        RuleFor(x => x.ToPaddockId).NotEqual(Guid.Empty).When(x => x.ToPaddockId.HasValue);
        RuleFor(x => x.ToLotId).NotEqual(Guid.Empty).When(x => x.ToLotId.HasValue);
        RuleFor(x => x.ExpectedFromPaddockId).NotEqual(Guid.Empty).When(x => x.ExpectedFromPaddockId.HasValue);
        RuleFor(x => x.ExpectedFromLotId).NotEqual(Guid.Empty).When(x => x.ExpectedFromLotId.HasValue);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}

public sealed class PaddockResidentPageRequestValidator : AbstractValidator<PaddockResidentPageRequest>
{
    public PaddockResidentPageRequestValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 100000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.LotId).NotEqual(Guid.Empty).When(x => x.LotId.HasValue);
        RuleFor(x => x.Ungrouped).Equal(false).When(x => x.LotId.HasValue);
    }
}
