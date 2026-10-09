using Core.Application.Operations;
using System.Net;
using System.Text.Json;
namespace Infrastructure.Operations;
public sealed class BcvRateProvider(HttpClient client, TimeProvider? timeProvider = null) : IBcvRateProvider
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    public async Task<BcvReference> GetAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(-4)).DateTime);
        var path = $"api/v1/history/{today:yyyy-MM-dd}.json";
        using var first = await client.GetAsync(path, ct);
        using var fallback = first.StatusCode == HttpStatusCode.NotFound ? await client.GetAsync("api/v1/rate.json", ct) : null;
        var response = fallback ?? first; response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = json.RootElement;
        if (!root.TryGetProperty("USD", out var amount) || !amount.TryGetDecimal(out var rate) || rate <= 0 || rate >= 1000000000m || decimal.Round(rate, 6) != rate ||
            !root.TryGetProperty("effective_date", out var effective) || !DateOnly.TryParseExact(effective.GetString(), "yyyy-MM-dd", out var date) || date > today || date < today.AddDays(-7) ||
            !root.TryGetProperty("updated_at", out var timestamp) || !DateTimeOffset.TryParse(timestamp.GetString(), out var published) || published > now.AddMinutes(5) || published < now.AddDays(-7))
            throw new InvalidDataException("The provider returned an invalid, future or stale BCV reference.");
        return new(date, rate, published.UtcDateTime, fallback is null ? "https://bcv.today/" + path : "https://bcv.today/api/v1/rate.json");
    }
}
