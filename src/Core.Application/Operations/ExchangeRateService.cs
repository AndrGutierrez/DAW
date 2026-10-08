using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;
namespace Core.Application.Operations;
public sealed record ExchangeRateRequest(Guid SubmissionId, DateOnly EffectiveDate, decimal BolivarsPerDollar);
public sealed record ExchangeRateQuote(Guid Id, DateOnly EffectiveDate, decimal BolivarsPerDollar, DateTime RecordedAt,
    string Source = "https://www.bcv.org.ve/", string EntryMethod = "manual");
public sealed class ExchangeRateRequestValidator : AbstractValidator<ExchangeRateRequest>
{
    public ExchangeRateRequestValidator()
    {
        RuleFor(q => q.SubmissionId).NotEmpty();
        RuleFor(q => q.EffectiveDate).NotEmpty().LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14));
        RuleFor(q => q.BolivarsPerDollar).GreaterThan(0).LessThan(1000000000m).PrecisionScale(18, 6, true);
    }
}
public sealed class ExchangeRateService(IManagementRepository repository, ICurrentUser user, IValidator<ExchangeRateRequest> validator)
{
    public async Task<ExchangeRateQuote?> GetAsync(DateOnly date, CancellationToken ct = default)
    {
        if (date == default || date > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Choose a current or past valuation date.");
        var rates = await repository.ListAsync<ExchangeRate>(r => r.EffectiveDate <= date, ct);
        var rate = rates.OrderByDescending(r => r.EffectiveDate).ThenByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).FirstOrDefault();
        return rate == null ? null : Read(rate);
    }
    public Task<ExchangeRateQuote> RecordAsync(ExchangeRateRequest q, CancellationToken ct = default) =>
        repository.ExecuteWriteAsync<ExchangeRateQuote>(async () =>
        {
            await validator.ValidateAndThrowAsync(q, ct);
            var existing = await repository.GetAsync<ExchangeRate>(q.SubmissionId, ct: ct);
            if (existing != null)
            {
                if (existing.EffectiveDate != q.EffectiveDate || existing.BolivarsPerDollar != q.BolivarsPerDollar)
                    throw new ConflictException("The submission identifier belongs to a different exchange rate.");
                return Read(existing);
            }
            var rate = new ExchangeRate(q.SubmissionId) { EffectiveDate = q.EffectiveDate, BolivarsPerDollar = q.BolivarsPerDollar, RecordedByUserId = user.UserId };
            repository.Add(rate); await repository.SaveAsync(ct); return Read(rate);
        }, ct);
    private static ExchangeRateQuote Read(ExchangeRate r) => new(r.Id, r.EffectiveDate, r.BolivarsPerDollar, r.CreatedAt);
}
