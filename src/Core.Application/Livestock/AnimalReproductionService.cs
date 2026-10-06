using System.Linq.Expressions;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;

namespace Core.Application.Livestock;

public sealed class AnimalReproductionService(IManagementRepository repository, IFarmAccess farms, ICurrentUser user,
    IValidator<ReproductiveRequest> validator, IValidator<CarePageRequest> pages)
{
    public async Task<ReproductiveHistory> GetAsync(Guid animalId, CarePageRequest page, CancellationToken ct = default)
    {
        await pages.ValidateAndThrowAsync(page, ct);
        var animal = await AnimalCareRules.FindAsync(repository, farms, animalId, false, ct);
        Expression<Func<ReproductiveEvent, bool>> scope = x => x.DamId == animal.Id && x.FarmId == animal.FarmId;
        var total = await repository.CountAsync(scope, ct);
        var records = await repository.PageAsync(scope, x => x.Date, (page.Page - 1) * page.PageSize, page.PageSize, ct);
        var significant = await repository.PageAsync<ReproductiveEvent, DateOnly>(x => x.DamId == animal.Id &&
            (x is PregnancyCheck || x is Calving || x is Abortion), x => x.Date, 0, 1, ct);
        var latest = significant.FirstOrDefault();
        var state = animal.Sex != Sex.Female ? "NotApplicable" : latest switch
        {
            PregnancyCheck x when x.Result == PregnancyResult.Positive => "Pregnant",
            PregnancyCheck x when x.Result == PregnancyResult.Negative => "NotPregnant",
            PregnancyCheck => "Uncertain",
            Calving => "Calved",
            Abortion => "Aborted",
            _ => "Unknown"
        };
        return new(new(records.Select(x => new ReproductiveRecord(x.Id, x.CreatedAt, x.UserId, Read(x))).ToList(), total, page.Page, page.PageSize),
            new(state, latest?.Date, (latest as PregnancyCheck)?.ExpectedCalvingDate));
    }
    public Task<CareSubmission<ReproductiveRequest>> RecordAsync(Guid animalId, ReproductiveRequest request, CancellationToken ct = default) =>
        repository.ExecuteWriteAsync<CareSubmission<ReproductiveRequest>>(async () =>
        {
            var q = request with { Notes = AnimalCareRules.Clean(request.Notes), Method = AnimalCareRules.Clean(request.Method), Reason = AnimalCareRules.Clean(request.Reason) };
            await validator.ValidateAndThrowAsync(q, ct);
            var animal = await AnimalCareRules.FindAsync(repository, farms, animalId, true, ct);
            var existing = await repository.GetAsync<ReproductiveEvent>(q.SubmissionId, ct: ct);
            if (existing != null)
            {
                AnimalCareRules.Check(existing.DamId == animal.Id && existing.FarmId == animal.FarmId && existing.UserId == user.UserId && Read(existing) == q,
                    "The submission identifier belongs to a different reproductive record.");
                return new(existing.Id, true, q);
            }
            AnimalCareRules.Check(animal.Status == AnimalStatus.Active && animal.Sex == Sex.Female, "Reproductive events require an active female animal.");
            AnimalCareRules.Check(animal.BirthDate == null || q.Date >= animal.BirthDate, "The event date precedes the animal birth.");
            if (q.SireId is Guid sireId)
            {
                var sire = await repository.GetAsync<Animal>(sireId, ct: ct) ?? throw new ArgumentException("The sire does not exist.");
                AnimalCareRules.Check(sire.Id != animal.Id && sire.FarmId == animal.FarmId && sire.SpeciesId == animal.SpeciesId && sire.Sex == Sex.Male &&
                    (sire.BirthDate == null || sire.BirthDate < q.Date), "The sire does not match the farm, species, sex or event date.");
            }
            if (q.OffspringId is Guid offspringId)
            {
                var offspring = await repository.GetAsync<Animal>(offspringId, ct: ct) ?? throw new ArgumentException("The offspring does not exist.");
                AnimalCareRules.Check(offspring.DamId == animal.Id && offspring.FarmId == animal.FarmId && offspring.SpeciesId == animal.SpeciesId &&
                    (offspring.BirthDate == null || offspring.BirthDate <= q.Date), "The offspring does not match the dam or event date.");
            }
            ReproductiveEvent entity = q.Kind switch
            {
                ReproductiveEventKind.Heat => new Heat(q.SubmissionId) { Method = q.Method },
                ReproductiveEventKind.Mating => new Mating(q.SubmissionId) { SireId = q.SireId, Method = ReproductionMethod.Natural },
                ReproductiveEventKind.Insemination => new Insemination(q.SubmissionId) { SireId = q.SireId },
                ReproductiveEventKind.PregnancyCheck => new PregnancyCheck(q.SubmissionId) { Result = q.Result!.Value, Method = q.Method, ExpectedCalvingDate = q.ExpectedCalvingDate },
                ReproductiveEventKind.Calving => new Calving(q.SubmissionId) { OffspringCount = q.OffspringCount!.Value, StillbornCount = q.StillbornCount!.Value, Difficulty = q.Difficulty },
                ReproductiveEventKind.Weaning => new Weaning(q.SubmissionId) { OffspringId = q.OffspringId, WeightKg = q.WeightKg },
                ReproductiveEventKind.Abortion => new Abortion(q.SubmissionId) { Reason = q.Reason },
                _ => throw new ArgumentException("Unsupported reproductive event.")
            };
            entity.FarmId = animal.FarmId; entity.DamId = animal.Id; entity.Date = q.Date; entity.Notes = q.Notes; entity.UserId = user.UserId;
            animal.UpdatedAt = DateTime.UtcNow;
            repository.Add(entity);
            await repository.SaveAsync(ct);
            return new(entity.Id, false, q);
        }, ct);
    public static ReproductiveRequest Read(ReproductiveEvent e) => e switch
    {
        Heat x => new(e.Id, ReproductiveEventKind.Heat, e.Date, e.Notes, Method: x.Method),
        Mating x => new(e.Id, ReproductiveEventKind.Mating, e.Date, e.Notes, x.SireId),
        Insemination x => new(e.Id, ReproductiveEventKind.Insemination, e.Date, e.Notes, x.SireId),
        PregnancyCheck x => new(e.Id, ReproductiveEventKind.PregnancyCheck, e.Date, e.Notes, Result: x.Result, Method: x.Method, ExpectedCalvingDate: x.ExpectedCalvingDate),
        Calving x => new(e.Id, ReproductiveEventKind.Calving, e.Date, e.Notes, OffspringCount: x.OffspringCount, StillbornCount: x.StillbornCount, Difficulty: x.Difficulty),
        Weaning x => new(e.Id, ReproductiveEventKind.Weaning, e.Date, e.Notes, OffspringId: x.OffspringId, WeightKg: x.WeightKg),
        Abortion x => new(e.Id, ReproductiveEventKind.Abortion, e.Date, e.Notes, Reason: x.Reason),
        _ => throw new ArgumentException("Unsupported reproductive event.")
    };
}
