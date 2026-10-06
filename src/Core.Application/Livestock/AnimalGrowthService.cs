using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;

namespace Core.Application.Livestock;

public sealed record GrowthPoint(Guid RecordId, DateOnly Date, decimal WeightKg, decimal? BodyConditionScore, decimal? DailyGainKg);
public sealed record WeightHistoryItem(Guid Id, DateOnly Date, decimal WeightKg, decimal? BodyConditionScore, string? Notes, DateTime CreatedAt, bool UsedForCurve);
public sealed record GrowthPageRequest(int Page = 1, int PageSize = 20);
public sealed class GrowthPageRequestValidator : AbstractValidator<GrowthPageRequest>
{
    public GrowthPageRequestValidator()
    {
        RuleFor(request => request.Page).InclusiveBetween(1, 100000);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
    }
}
public sealed record AnimalGrowthResult(IReadOnlyList<GrowthPoint> Points, IReadOnlyList<WeightHistoryItem> Records, int Total, int Page, int PageSize, int TotalDates);

public sealed class AnimalGrowthService(IManagementRepository repository, IFarmAccess farms)
{
    public async Task<AnimalGrowthResult> GetAsync(Guid animalId, GrowthPageRequest? request = null, CancellationToken ct = default)
    {
        request ??= new GrowthPageRequest();
        await new GrowthPageRequestValidator().ValidateAndThrowAsync(request, ct);
        var animal = await repository.GetAsync<Animal>(animalId, false, ct)
            ?? throw new KeyNotFoundException("The requested animal was not found.");
        if (!await farms.CanAccessAsync(animal.FarmId, ct))
            throw new KeyNotFoundException("The requested animal was not found.");

        var records = await repository.ListAsync<WeightRecord>(
            record => record.AnimalId == animalId && record.FarmId == animal.FarmId, ct);
        // Multiple observations on one date stay in history. The latest is the daily representative.
        var daily = records.GroupBy(record => record.Date)
            .Select(group => group.OrderByDescending(record => record.CreatedAt).ThenByDescending(record => record.Id).First())
            .OrderBy(record => record.Date).ToArray();
        var points = new List<GrowthPoint>();
        for (var index = 0; index < daily.Length; index++)
        {
            var current = daily[index];
            decimal? gain = index == 0 ? null : decimal.Round(
                (current.WeightKg - daily[index - 1].WeightKg) / (current.Date.DayNumber - daily[index - 1].Date.DayNumber),
                4, MidpointRounding.AwayFromZero);
            points.Add(new(current.Id, current.Date, current.WeightKg, current.BodyConditionScore, gain));
        }
        var selected = daily.Select(record => record.Id).ToHashSet();
        return new(points.TakeLast(60).ToArray(), records.OrderByDescending(record => record.Date)
            .ThenByDescending(record => record.CreatedAt).ThenByDescending(record => record.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(record => new WeightHistoryItem(record.Id, record.Date, record.WeightKg,
                record.BodyConditionScore, record.Notes, record.CreatedAt, selected.Contains(record.Id))).ToArray(), records.Count, request.Page, request.PageSize, daily.Length);
    }
}
