using FluentValidation;
using Core.Domain.Livestock;

namespace Core.Application.Management;

public sealed class FarmRequestValidator : AbstractValidator<FarmRequest>
{
    public FarmRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class SpeciesRequestValidator : AbstractValidator<SpeciesRequest>
{
    public SpeciesRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Purpose).IsInEnum();
        RuleFor(x => x.GestationDays).GreaterThan(0).When(x => x.GestationDays.HasValue);
    }
}

public sealed class BreedRequestValidator : AbstractValidator<BreedRequest>
{
    public BreedRequestValidator()
    {
        RuleFor(x => x.SpeciesId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Purpose).IsInEnum();
        RuleFor(x => x.Origin).MaximumLength(100);
    }
}

public sealed class PaddockRequestValidator : AbstractValidator<PaddockRequest>
{
    public PaddockRequestValidator()
    {
        RuleFor(x => x.FarmId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Code).MaximumLength(30);
        RuleFor(x => x.AreaHectares).GreaterThan(0).PrecisionScale(14, 4, false).When(x => x.AreaHectares.HasValue);
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
    }
}

public sealed class LotRequestValidator : AbstractValidator<LotRequest>
{
    public LotRequestValidator()
    {
        RuleFor(x => x.FarmId).NotEmpty();
        RuleFor(x => x.SpeciesId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Purpose).IsInEnum();
        RuleFor(x => x.PaddockId).NotEqual(Guid.Empty).When(x => x.PaddockId.HasValue);
    }
}

public sealed class CategoryRequestValidator : AbstractValidator<CategoryRequest>
{
    public CategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class ProductRequestValidator : AbstractValidator<ProductRequest>
{
    public ProductRequestValidator()
    {
        RuleFor(x => x.SKU).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0).PrecisionScale(18, 2, false);
        RuleFor(x => x.CostPrice).GreaterThan(0).PrecisionScale(18, 2, false);
        RuleFor(x => x.Unit).IsInEnum();
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100);
        RuleFor(x => x.WithdrawalDays).GreaterThanOrEqualTo(0).When(x => x.WithdrawalDays.HasValue);
    }
}

public sealed class InventoryRequestValidator : AbstractValidator<InventoryRequest>
{
    public InventoryRequestValidator()
    {
        RuleFor(x => x.FarmId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).PrecisionScale(14, 4, false);
        RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0).PrecisionScale(14, 4, false);
        RuleFor(x => x.MaxStock).GreaterThan(x => x.MinStock).PrecisionScale(14, 4, false);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(150);
    }
}

public sealed class AnimalRequestValidator : AbstractValidator<AnimalRequest>
{
    public AnimalRequestValidator()
    {
        RuleFor(x => x.FarmId).NotEmpty();
        RuleFor(x => x.SpeciesId).NotEmpty();
        RuleFor(x => x.InternalTag).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Sex).IsInEnum();
        RuleFor(x => x.Purpose).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Origin).IsInEnum();
        RuleFor(x => x.HealthStatus).IsInEnum();
        RuleFor(x => x.BirthDate).LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow)).When(x => x.BirthDate.HasValue);
        RuleFor(x => x.BirthWeightKg).GreaterThan(0).PrecisionScale(14, 4, false).When(x => x.BirthWeightKg.HasValue);
        RuleFor(x => x.OfficialId).MaximumLength(50);
        RuleFor(x => x.Rfid).MaximumLength(50);
        RuleFor(x => x.Name).MaximumLength(100);
        RuleFor(x => x.Color).MaximumLength(50);
        RuleFor(x => x.Markings).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.BreedId).NotEqual(Guid.Empty).When(x => x.BreedId.HasValue);
        RuleFor(x => x.LotId).NotEqual(Guid.Empty).When(x => x.LotId.HasValue);
        RuleFor(x => x.PaddockId).NotEqual(Guid.Empty).When(x => x.PaddockId.HasValue);
        RuleFor(x => x.DamId).NotEqual(Guid.Empty).When(x => x.DamId.HasValue);
        RuleFor(x => x.SireId).NotEqual(Guid.Empty).When(x => x.SireId.HasValue);
    }
}

public sealed class WeightRequestValidator : AbstractValidator<WeightRequest>
{
    public WeightRequestValidator()
    {
        RuleFor(x => x.FarmId).NotEmpty();
        RuleFor(x => x.AnimalId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty().LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.WeightKg).GreaterThan(0).PrecisionScale(8, 2, false);
        RuleFor(x => x.BodyConditionScore).InclusiveBetween(1, 5).PrecisionScale(4, 2, false).When(x => x.BodyConditionScore.HasValue);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class HealthUpdateRequestValidator : AbstractValidator<HealthUpdateRequest>
{
    public HealthUpdateRequestValidator()
    {
        RuleFor(x => x.HealthStatus).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class LegacyHealthUpdateRequestValidator : AbstractValidator<LegacyHealthUpdateRequest>
{
    public LegacyHealthUpdateRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class ProductionRequestValidator : AbstractValidator<ProductionRequest>
{
    public ProductionRequestValidator()
    {
        RuleFor(x => x.FarmId).NotEmpty();
        RuleFor(x => x.AnimalId).NotEmpty();
        RuleFor(x => x.OperationId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty().LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.ProductType).IsInEnum();
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Unit).IsInEnum();
        RuleFor(x => x.Quantity).GreaterThan(0).PrecisionScale(14, 4, false);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x).Must(x => x.ProductType switch
        {
            AnimalProductType.Milk => x.Method == ProductionMethod.Milking && x.Unit == MeasurementUnit.Liter,
            AnimalProductType.Wool => x.Method == ProductionMethod.Shearing && x.Unit == MeasurementUnit.Kilogram,
            AnimalProductType.Meat => x.Method == ProductionMethod.Slaughter && x.Unit == MeasurementUnit.Kilogram,
            AnimalProductType.Hide => x.Method == ProductionMethod.Slaughter && x.Unit == MeasurementUnit.Unit,
            AnimalProductType.Eggs => x.Method == ProductionMethod.Collection && x.Unit == MeasurementUnit.Unit,
            AnimalProductType.Other => x.Method is ProductionMethod.Collection or ProductionMethod.Slaughter && x.Unit is MeasurementUnit.Unit or MeasurementUnit.Kilogram or MeasurementUnit.Liter,
            _ => false
        }).WithMessage("The product type, production method and measurement unit are incompatible.");
        RuleFor(x => x.Quantity).Must(q => q == decimal.Truncate(q)).When(x => x.Unit == MeasurementUnit.Unit).WithMessage("Unit quantities must be whole numbers.");
    }
}
