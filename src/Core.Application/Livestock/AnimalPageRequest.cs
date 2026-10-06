using Core.Domain.Livestock;
using FluentValidation;

namespace Core.Application.Livestock;

public sealed record AnimalPageRequest(
    int Page = 1, int PageSize = 12, string? Search = null, AnimalStatus? Status = null, Guid? FarmId = null);

public sealed record AnimalPageResult(IReadOnlyList<AnimalListItem> Items, int Total, int Page, int PageSize);

public sealed class AnimalPageRequestValidator : AbstractValidator<AnimalPageRequest>
{
    public AnimalPageRequestValidator()
    {
        RuleFor(request => request.Page).InclusiveBetween(1, 100000);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Search).MaximumLength(100);
        RuleFor(request => request.Status).IsInEnum().When(request => request.Status.HasValue);
        RuleFor(request => request.FarmId).NotEqual(Guid.Empty).When(request => request.FarmId.HasValue);
    }
}
