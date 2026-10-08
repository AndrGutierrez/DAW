using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;

namespace Core.Application.Livestock;

public sealed record GrowthGoalRequest(decimal? DailyGainKg);
public sealed record GrowthGoal(decimal? DailyGainKg, string Source);
public sealed record GrowthObservation(Guid AnimalId, Guid FarmId, string Tag, string? Name, decimal? IndividualGoal,
    DateOnly? CurrentDate, decimal? CurrentWeight, DateOnly? PreviousDate, decimal? PreviousWeight);
public sealed record GrowthAlert(Guid AnimalId, Guid FarmId, string Tag, string? Name, DateOnly PreviousDate, DateOnly CurrentDate,
    decimal PreviousWeight, decimal CurrentWeight, decimal DailyGainKg, decimal? TargetDailyGainKg, string TargetSource, string Reason);
public sealed record GrowthAlertsResult(IReadOnlyList<GrowthAlert> Items, int Total, int Page, int PageSize, int ActiveBovines, int InsufficientMeasurements);
public interface IGrowthMonitoringReader
{
    Task<IReadOnlyList<GrowthObservation>> ReadAsync(IReadOnlyCollection<Guid> farmIds, CancellationToken ct);
}
public sealed class GrowthGoalRequestValidator : AbstractValidator<GrowthGoalRequest>
{
    public GrowthGoalRequestValidator() => RuleFor(q => q.DailyGainKg).InclusiveBetween(0, 1000).PrecisionScale(8, 4, false).When(q => q.DailyGainKg.HasValue);
}

public sealed class GrowthMonitoringService(IManagementRepository repository, IFarmAccess farms, IGrowthMonitoringReader reader)
{
    public static GrowthGoal Resolve(decimal? individual, AlertRule? policy) => individual.HasValue
        ? new(individual, "animal") : policy is { IsEnabled: true, ThresholdValue: not null }
            ? new(policy.ThresholdValue, "farm") : new(null, "none");

    public async Task<GrowthGoal> PolicyAsync(Guid farmId, CancellationToken ct = default)
    {
        await RequireFarm(farmId, ct);
        return Resolve(null, (await repository.ListAsync<AlertRule>(r => r.FarmId == farmId && r.Type == AlertType.LowWeightGain, ct))
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).FirstOrDefault());
    }

    public Task<GrowthGoal> SetPolicyAsync(Guid farmId, GrowthGoalRequest request, CancellationToken ct = default) => repository.ExecuteWriteAsync(async () =>
    {
        await new GrowthGoalRequestValidator().ValidateAndThrowAsync(request, ct);
        await RequireFarm(farmId, ct);
        var rule = (await repository.ListAsync<AlertRule>(r => r.FarmId == farmId && r.Type == AlertType.LowWeightGain, ct))
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).FirstOrDefault();
        if (rule == null) { rule = new AlertRule { FarmId = farmId, Type = AlertType.LowWeightGain }; repository.Add(rule); }
        else rule = (await repository.GetAsync<AlertRule>(rule.Id, true, ct))!;
        rule.ThresholdValue = request.DailyGainKg; rule.IsEnabled = request.DailyGainKg.HasValue;
        await repository.SaveAsync(ct); return Resolve(null, rule);
    }, ct);

    public Task<GrowthGoal> SetAnimalGoalAsync(Guid id, GrowthGoalRequest request, CancellationToken ct = default) => repository.ExecuteWriteAsync(async () =>
    {
        await new GrowthGoalRequestValidator().ValidateAndThrowAsync(request, ct);
        var animal = await repository.GetAsync<Animal>(id, true, ct) ?? throw new KeyNotFoundException("The animal was not found.");
        await RequireFarm(animal.FarmId, ct);
        animal.TargetDailyGainKg = request.DailyGainKg; await repository.SaveAsync(ct);
        return request.DailyGainKg.HasValue ? Resolve(request.DailyGainKg, null) : await PolicyAsync(animal.FarmId, ct);
    }, ct);

    public async Task<GrowthAlertsResult> AlertsAsync(Guid? farmId, CarePageRequest query, CancellationToken ct = default)
    {
        await new CarePageRequestValidator().ValidateAndThrowAsync(query, ct);
        var ids = await farms.GetAccessibleFarmIdsAsync(ct);
        if (farmId.HasValue) { await RequireFarm(farmId.Value, ct); ids = [farmId.Value]; }
        var observations = await reader.ReadAsync(ids, ct);
        var policies = (await repository.ListAsync<AlertRule>(r => ids.Contains(r.FarmId) && r.Type == AlertType.LowWeightGain, ct))
            .GroupBy(r => r.FarmId).ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).First());
        var alerts = new List<GrowthAlert>(); var insufficient = 0;
        foreach (var row in observations)
        {
            if (!row.CurrentDate.HasValue || !row.PreviousDate.HasValue || !row.CurrentWeight.HasValue || !row.PreviousWeight.HasValue || row.CurrentDate <= row.PreviousDate) { insufficient++; continue; }
            var rawGain = (row.CurrentWeight.Value - row.PreviousWeight.Value) / (row.CurrentDate.Value.DayNumber - row.PreviousDate.Value.DayNumber);
            policies.TryGetValue(row.FarmId, out var policy); var goal = Resolve(row.IndividualGoal, policy);
            if (rawGain < 0 || goal.DailyGainKg.HasValue && rawGain < goal.DailyGainKg)
                alerts.Add(new(row.AnimalId, row.FarmId, row.Tag, row.Name, row.PreviousDate.Value, row.CurrentDate.Value, row.PreviousWeight.Value, row.CurrentWeight.Value,
                    decimal.Round(rawGain, 4, MidpointRounding.AwayFromZero), goal.DailyGainKg, goal.Source, rawGain < 0 ? "weight-loss" : "below-target"));
        }
        var ordered = alerts.OrderBy(a => a.Reason == "weight-loss" ? 0 : 1).ThenBy(a => a.DailyGainKg).ThenBy(a => a.Tag).ThenBy(a => a.AnimalId).ToArray();
        return new(ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToArray(), ordered.Length, query.Page, query.PageSize, observations.Count, insufficient);
    }

    private async Task RequireFarm(Guid id, CancellationToken ct)
    {
        if (!await farms.CanAccessAsync(id, ct) || await repository.GetAsync<Farm>(id, ct: ct) == null) throw new KeyNotFoundException("The farm was not found.");
    }
}
