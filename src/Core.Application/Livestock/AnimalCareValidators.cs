using FluentValidation;

namespace Core.Application.Livestock;

public sealed class CarePageRequestValidator : AbstractValidator<CarePageRequest>
{
    public CarePageRequestValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 100000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
public sealed class ClinicalRequestValidator : AbstractValidator<ClinicalRequest>
{
    public ClinicalRequestValidator()
    {
        RuleFor(x => x.SubmissionId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum().NotEqual(ClinicalEventKind.Mortality);
        RuleFor(x => x.Date).NotEmpty().LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.ProductId).NotEqual(Guid.Empty).When(x => x.ProductId.HasValue);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, false).When(x => x.Cost.HasValue);
        RuleFor(x => x.Dose).GreaterThan(0).PrecisionScale(10, 3, false).When(x => x.Dose.HasValue);
        RuleFor(x => x.Route).IsInEnum();
        RuleFor(x => x.WithdrawalDays).InclusiveBetween(0, 3650).When(x => x.WithdrawalDays.HasValue);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.Date).When(x => x.EndDate.HasValue);
        RuleFor(x => x.NextDueDate).GreaterThan(x => x.Date).When(x => x.NextDueDate.HasValue);
        RuleFor(x => x.Severity).MaximumLength(50);
        RuleFor(x => x.Reason).MaximumLength(500);
        When(x => x.Kind == ClinicalEventKind.Treatment, () =>
        {
            RuleFor(x => x.ProductId).NotNull();
            RuleFor(x => x.Dose).NotNull();
            RuleFor(x => x.EndDate).NotNull().LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow));
        });
        When(x => x.Kind == ClinicalEventKind.Quarantine, () =>
        {
            RuleFor(x => x.Reason).NotEmpty();
        });
        RuleFor(x => x).Custom((x, context) =>
        {
            if (x.Kind != ClinicalEventKind.Treatment && (x.WithdrawalDays != null || x.Route != Core.Domain.Livestock.MedicationRoute.Other))
                context.AddFailure("Kind", "Withdrawal and route fields belong to treatments.");
            if (x.Kind is not (ClinicalEventKind.Treatment or ClinicalEventKind.Quarantine) && x.EndDate != null)
                context.AddFailure("EndDate", "End date belongs to treatments or quarantine.");
            if (x.Kind != ClinicalEventKind.Vaccination && x.NextDueDate != null)
                context.AddFailure("NextDueDate", "Next due date belongs to vaccinations.");
            if (x.Kind != ClinicalEventKind.DiseaseCase && (x.Severity != null || x.IsContagious))
                context.AddFailure("Severity", "Disease fields belong to disease cases.");
            if (x.Kind != ClinicalEventKind.Quarantine && x.Reason != null)
                context.AddFailure("Reason", "Reason belongs to quarantine.");
            if (x.Kind is ClinicalEventKind.DiseaseCase or ClinicalEventKind.Quarantine && (x.ProductId != null || x.Dose != null))
                context.AddFailure("ProductId", "This event does not administer a product.");
        });
    }
}
public sealed class ReproductiveRequestValidator : AbstractValidator<ReproductiveRequest>
{
    public ReproductiveRequestValidator()
    {
        RuleFor(x => x.SubmissionId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Date).NotEmpty().LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Method).MaximumLength(50);
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.SireId).NotEqual(Guid.Empty).When(x => x.SireId.HasValue);
        RuleFor(x => x.OffspringId).NotEqual(Guid.Empty).When(x => x.OffspringId.HasValue);
        RuleFor(x => x.Result).IsInEnum().When(x => x.Result.HasValue);
        RuleFor(x => x.Difficulty).IsInEnum();
        RuleFor(x => x.ExpectedCalvingDate).GreaterThan(x => x.Date).When(x => x.ExpectedCalvingDate.HasValue);
        RuleFor(x => x.WeightKg).GreaterThan(0).PrecisionScale(8, 2, false).When(x => x.WeightKg.HasValue);
        When(x => x.Kind == ReproductiveEventKind.PregnancyCheck, () => RuleFor(x => x.Result).NotNull());
        When(x => x.Kind == ReproductiveEventKind.Calving, () =>
        {
            RuleFor(x => x.OffspringCount).NotNull().InclusiveBetween(1, 100);
            RuleFor(x => x.StillbornCount).NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(x => x.OffspringCount);
        });
        RuleFor(x => x).Custom((x, context) =>
        {
            if (x.Kind is not (ReproductiveEventKind.Mating or ReproductiveEventKind.Insemination) && x.SireId != null)
                context.AddFailure("SireId", "Sire belongs to mating or insemination.");
            if (x.Kind != ReproductiveEventKind.PregnancyCheck && (x.Result != null || x.ExpectedCalvingDate != null))
                context.AddFailure("Result", "Pregnancy fields belong to a pregnancy check.");
            if (x.Result is not Core.Domain.Livestock.PregnancyResult.Positive && x.ExpectedCalvingDate != null)
                context.AddFailure("ExpectedCalvingDate", "Only a positive check has an expected calving date.");
            if (x.Kind != ReproductiveEventKind.Calving && (x.OffspringCount != null || x.StillbornCount != null || x.Difficulty != Core.Domain.Livestock.CalvingDifficulty.Easy))
                context.AddFailure("OffspringCount", "Calving fields belong to calving.");
            if (x.Kind != ReproductiveEventKind.Weaning && (x.OffspringId != null || x.WeightKg != null))
                context.AddFailure("OffspringId", "Offspring fields belong to weaning.");
            if (x.Kind != ReproductiveEventKind.Abortion && x.Reason != null)
                context.AddFailure("Reason", "Reason belongs to abortion.");
            if (x.Kind is not (ReproductiveEventKind.Heat or ReproductiveEventKind.PregnancyCheck) && x.Method != null)
                context.AddFailure("Method", "Observation method belongs to heat or pregnancy checks.");
        });
    }
}
