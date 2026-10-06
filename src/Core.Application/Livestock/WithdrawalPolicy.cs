using Core.Application.Management;
using Core.Domain.Livestock;

namespace Core.Application.Livestock;

// Date-only policy: the final restricted day is inclusive, including a zero-day administration.
public sealed class WithdrawalPolicy(IManagementRepository repository)
{
    public async Task<WithdrawalStatus> GetAsync(Guid animalId, DateOnly date, CancellationToken ct = default)
    {
        var treatments = await repository.ListAsync<Treatment>(x => x.AnimalId == animalId && x.Date <= date, ct);
        var count = 0;
        DateOnly? latest = null;
        var unknown = false;
        foreach (var treatment in treatments)
        {
            var days = treatment.WithdrawalDays;
            var end = treatment.WithdrawalEndDate;
            if (days >= 0 && days <= 3650)
            {
                var calculated = (treatment.EndDate ?? treatment.StartDate ?? treatment.Date).AddDays(days.Value);
                if (end == null || calculated > end) end = calculated;
            }
            if (end == null) { unknown = true; count++; }
            else if (end >= date) { count++; if (latest == null || end > latest) latest = end; }
        }
        return new(count > 0, unknown ? null : latest, unknown || latest == null ? null : latest.Value.AddDays(1), count);
    }
    public async Task CheckAsync(Guid animalId, DateOnly date, CancellationToken ct)
    {
        if ((await GetAsync(animalId, date, ct)).Blocked)
            throw new ConflictException("Milk and slaughter are blocked by a medication withdrawal period on this date.");
    }
}
