using FluentValidation;

namespace Core.Application.Livestock;

public sealed record AnimalMovementBatchItem(Guid AnimalId, Guid SubmissionId, Guid? ExpectedFromPaddockId, Guid? ExpectedFromLotId);
public sealed record AnimalMovementBatchRequest(Guid FarmId, Guid ToPaddockId, string Reason,
    IReadOnlyList<AnimalMovementBatchItem> Animals, bool ChangeLot = false, Guid? ToLotId = null);
public sealed record AnimalMovementBatchItemResult(Guid AnimalId, Guid Id, bool Replayed, AnimalMovementRecord Data);
public sealed record AnimalMovementBatchResult(IReadOnlyList<AnimalMovementBatchItemResult> Items, bool Replayed);

public sealed class AnimalMovementBatchRequestValidator : AbstractValidator<AnimalMovementBatchRequest>
{
    public AnimalMovementBatchRequestValidator()
    {
        RuleFor(x => x.FarmId).NotEmpty();
        RuleFor(x => x.ToPaddockId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ToLotId).Null().When(x => !x.ChangeLot);
        RuleFor(x => x.ToLotId).NotEqual(Guid.Empty).When(x => x.ToLotId.HasValue);
        RuleFor(x => x.Animals).NotEmpty();
        When(x => x.Animals is not null, () =>
        {
            RuleFor(x => x.Animals.Count).InclusiveBetween(1, 500);
            RuleForEach(x => x.Animals).NotNull().SetValidator(new AnimalMovementBatchItemValidator());
            RuleFor(x => x.Animals).Must(items => items.All(x => x is not null) &&
                items.Select(x => x.AnimalId).Distinct().Count() == items.Count &&
                items.Select(x => x.SubmissionId).Distinct().Count() == items.Count)
                .WithMessage("Each animal and submission identifier must occur only once.");
        });
    }
}
public sealed class AnimalMovementBatchItemValidator : AbstractValidator<AnimalMovementBatchItem>
{
    public AnimalMovementBatchItemValidator()
    {
        RuleFor(x => x.AnimalId).NotEmpty();
        RuleFor(x => x.SubmissionId).NotEmpty();
        RuleFor(x => x.ExpectedFromPaddockId).NotEqual(Guid.Empty).When(x => x.ExpectedFromPaddockId.HasValue);
        RuleFor(x => x.ExpectedFromLotId).NotEqual(Guid.Empty).When(x => x.ExpectedFromLotId.HasValue);
    }
}
