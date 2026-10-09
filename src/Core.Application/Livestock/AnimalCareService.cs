using System.Linq.Expressions;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;

namespace Core.Application.Livestock;

public sealed class AnimalCareService(IManagementRepository repository, IFarmAccess farms, ICurrentUser user,
    IValidator<ClinicalRequest> validator, IValidator<CarePageRequest> pages, WithdrawalPolicy withdrawal)
{
    public async Task<ClinicalHistory> GetAsync(Guid animalId, CarePageRequest page, CancellationToken ct = default)
    {
        await pages.ValidateAndThrowAsync(page, ct);
        var animal = await AnimalCareRules.FindAsync(repository, farms, animalId, false, ct);
        Expression<Func<HealthEvent, bool>> scope = x => x.AnimalId == animal.Id && x.FarmId == animal.FarmId;
        var total = await repository.CountAsync(scope, ct);
        var records = await repository.PageAsync(scope, x => x.Date, (page.Page - 1) * page.PageSize, page.PageSize, ct);
        var productIds = records.Select(Read).Where(x => x.ProductId != null).Select(x => x.ProductId!.Value).Distinct().ToList();
        var products = await repository.ListAsync<Product>(x => productIds.Contains(x.Id), ct);
        var productNames = products.ToDictionary(x => x.Id, x => x.Name + " · " + x.SKU);
        var changes = await repository.PageAsync<HealthStatusChange, DateTime>(x => x.AnimalId == animal.Id && x.FarmId == animal.FarmId, x => x.ChangedAt, 0, 20, ct);
        return new(new(records.Select(x => new ClinicalRecord(x.Id, x.CreatedAt, x.UserId, Read(x), (x as Treatment)?.WithdrawalEndDate, Read(x).ProductId is Guid productId && productNames.TryGetValue(productId, out var name) ? name : null)).ToList(), total, page.Page, page.PageSize),
            await withdrawal.GetAsync(animal.Id, DateOnly.FromDateTime(DateTime.UtcNow), ct),
            changes.Select(x => new HealthChangeRecord(x.ChangedAt, x.PreviousStatus, x.NewStatus, x.Reason)).ToList());
    }
    public Task<CareSubmission<ClinicalRequest>> RecordAsync(Guid animalId, ClinicalRequest request, CancellationToken ct = default) =>
        repository.ExecuteWriteAsync<CareSubmission<ClinicalRequest>>(async () =>
        {
            var q = Normalize(request);
            await validator.ValidateAndThrowAsync(q, ct);
            var animal = await AnimalCareRules.FindAsync(repository, farms, animalId, true, ct);
            var existing = await repository.GetAsync<HealthEvent>(q.SubmissionId, ct: ct);
            if (existing != null)
            {
                var stored = Read(existing);
                var replay = q with { WithdrawalDays = q.WithdrawalDays ?? stored.WithdrawalDays };
                AnimalCareRules.Check(existing.AnimalId == animal.Id && existing.FarmId == animal.FarmId && existing.UserId == user.UserId && stored == replay,
                    "The submission identifier belongs to a different clinical record.");
                return new(existing.Id, true, stored);
            }
            AnimalCareRules.Check(animal.Status == AnimalStatus.Active, "Only active animals can register care events.");
            AnimalCareRules.Check(animal.BirthDate == null || q.Date >= animal.BirthDate, "The event date precedes the animal birth.");
            if (q.ProductId is Guid productId)
            {
                var product = await repository.GetAsync<Product>(productId, ct: ct) ?? throw new ArgumentException("The product does not exist.");
                AnimalCareRules.Check(product.IsActive, "The product is inactive.");
                if (q.Kind is ClinicalEventKind.Vaccination or ClinicalEventKind.Deworming && product.WithdrawalDays is not 0)
                    AnimalCareRules.Check(await repository.ExistsAsync<Treatment>(x => x.AnimalId == animal.Id && x.ProductId == productId &&
                        x.Date <= q.Date && x.EndDate >= q.Date && x.WithdrawalDays != null && (product.WithdrawalDays == null || x.WithdrawalDays >= product.WithdrawalDays), ct),
                        "A product with unknown or positive withdrawal needs a matching treatment record before vaccination or deworming.");
                if (q.Kind == ClinicalEventKind.Treatment)
                {
                    var days = q.WithdrawalDays ?? product.WithdrawalDays;
                    AnimalCareRules.Check(days is >= 0 and <= 3650, "Specify a known withdrawal period for this treatment.");
                    AnimalCareRules.Check(product.WithdrawalDays == null || days >= product.WithdrawalDays, "A treatment cannot shorten the product withdrawal period.");
                    q = q with { WithdrawalDays = days };
                }
            }
            HealthEvent entity = q.Kind switch
            {
                ClinicalEventKind.Treatment => new Treatment(q.SubmissionId) { ProductId = q.ProductId, Dose = q.Dose, Route = q.Route, StartDate = q.Date, EndDate = q.EndDate,
                    WithdrawalDays = q.WithdrawalDays, WithdrawalEndDate = q.EndDate!.Value.AddDays(q.WithdrawalDays!.Value) },
                ClinicalEventKind.Vaccination => new Vaccination(q.SubmissionId) { ProductId = q.ProductId, Dose = q.Dose, NextDueDate = q.NextDueDate },
                ClinicalEventKind.Deworming => new Deworming(q.SubmissionId) { ProductId = q.ProductId, Dose = q.Dose },
                ClinicalEventKind.DiseaseCase => new DiseaseCase(q.SubmissionId) { Severity = q.Severity, IsContagious = q.IsContagious },
                ClinicalEventKind.Quarantine => new Quarantine(q.SubmissionId) { StartDate = q.Date, EndDate = q.EndDate, Reason = q.Reason },
                _ => throw new ArgumentException("Unsupported clinical event.")
            };
            if (entity is Treatment treatment)
                AnimalCareRules.Check(!await repository.ExistsAsync<AnimalProduction>(x => x.AnimalId == animal.Id &&
                    (x.Method == ProductionMethod.Milking || x.Method == ProductionMethod.Slaughter) && x.Date >= q.Date && x.Date <= treatment.WithdrawalEndDate, ct),
                    "This treatment conflicts with existing milk or slaughter during withdrawal. Review the historical records first.");
            entity.FarmId = animal.FarmId; entity.AnimalId = animal.Id; entity.Date = q.Date;
            entity.UserId = user.UserId; entity.Notes = q.Notes; entity.Cost = q.Cost;
            animal.UpdatedAt = DateTime.UtcNow;
            repository.Add(entity);
            await repository.SaveAsync(ct);
            return new(entity.Id, false, q);
        }, ct);
    public static ClinicalRequest Read(HealthEvent e) => e switch
    {
        Treatment x => new(e.Id, ClinicalEventKind.Treatment, e.Date, e.Notes, x.ProductId, x.Dose, x.Route, x.EndDate, x.WithdrawalDays, Cost: e.Cost),
        Vaccination x => new(e.Id, ClinicalEventKind.Vaccination, e.Date, e.Notes, x.ProductId, x.Dose, NextDueDate: x.NextDueDate, Cost: e.Cost),
        Deworming x => new(e.Id, ClinicalEventKind.Deworming, e.Date, e.Notes, x.ProductId, x.Dose, Cost: e.Cost),
        DiseaseCase x => new(e.Id, ClinicalEventKind.DiseaseCase, e.Date, e.Notes, Severity: x.Severity, IsContagious: x.IsContagious, Cost: e.Cost),
        Quarantine x => new(e.Id, ClinicalEventKind.Quarantine, e.Date, e.Notes, EndDate: x.EndDate, Reason: x.Reason, Cost: e.Cost),
        MortalityEvent x => new(e.Id, ClinicalEventKind.Mortality, e.Date, e.Notes, Reason: x.Cause, Cost: e.Cost),
        _ => throw new ArgumentException("Unsupported clinical event.")
    };
    private static ClinicalRequest Normalize(ClinicalRequest q) => q with { Notes = AnimalCareRules.Clean(q.Notes), Reason = AnimalCareRules.Clean(q.Reason), Severity = AnimalCareRules.Clean(q.Severity) };
}
internal static class AnimalCareRules
{
    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public static void Check(bool valid, string message) { if (!valid) throw new ConflictException(message); }
    public static async Task<Animal> FindAsync(IManagementRepository repository, IFarmAccess farms, Guid id, bool tracking, CancellationToken ct)
    {
        var animal = await repository.GetAsync<Animal>(id, tracking, ct) ?? throw new KeyNotFoundException("The requested animal was not found.");
        if (!await farms.CanAccessAsync(animal.FarmId, ct)) throw new KeyNotFoundException("The requested animal was not found.");
        return animal;
    }
}
