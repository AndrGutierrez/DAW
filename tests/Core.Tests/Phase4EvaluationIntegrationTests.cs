using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Application.Operations;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
namespace Core.Tests;
public sealed partial class ManagementIntegrationTests
{
    [Fact]
    public async Task ExchangeRatesRequireAdminKeepHistoryAndNeverApplyAFutureQuote()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-4));
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/exchange-rates/usd-ves")).StatusCode);
        var q = new ExchangeRateRequest(Guid.NewGuid(), today.AddDays(-1), 41.123456m);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/exchange-rates/usd-ves", q)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/exchange-rates/usd-ves", q)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/exchange-rates/usd-ves", q with { BolivarsPerDollar = 42 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/exchange-rates/usd-ves", q with { SubmissionId = Guid.NewGuid(), BolivarsPerDollar = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/exchange-rates/usd-ves", new ExchangeRateRequest(Guid.NewGuid(), today.AddDays(1), 50))).StatusCode);
        var quote = await client.GetFromJsonAsync<ExchangeRateQuote>("/api/exchange-rates/usd-ves"); Assert.Equal(q.SubmissionId, quote!.Id); Assert.Equal(41.123456m, quote.BolivarsPerDollar);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await db.ExchangeRates.CountAsync()); Assert.True(await db.AuditLogs.AnyAsync(a => a.EntityName == nameof(ExchangeRate) && a.EntityId == q.SubmissionId.ToString()));
        await Login(client, "employee"); Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/exchange-rates/usd-ves")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/exchange-rates/usd-ves", q)).StatusCode);
    }
    [Fact]
    public async Task LiveAnalyticsEndpointRejectsAnonymousAndEmployeeConnections()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/analytics/changes")).StatusCode);
        await Login(client, "employee"); Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/analytics/changes")).StatusCode);
    }
    [Fact]
    public async Task ReproductiveMetricsUseBovineFemalesAndExcludeUnassignedFarmEvents()
    {
        using var factory = Factory(); await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var female = await db.Animals.Include(a => a.Species).FirstAsync(a => a.Sex == Sex.Female && a.Species.Code == "BO");
        var male = await db.Animals.FirstAsync(a => a.Sex == Sex.Male && a.Species.Code == "BO"); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.ReproductiveEvents.AddRange(new Mating { FarmId = female.FarmId, DamId = female.Id, Date = today.AddDays(-2) },
            new PregnancyCheck { FarmId = female.FarmId, DamId = female.Id, Date = today, Result = PregnancyResult.Positive },
            new PregnancyCheck { FarmId = male.FarmId, DamId = male.Id, Date = today, Result = PregnancyResult.Negative });
        await db.SaveChangesAsync();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web); options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        var result = await client.GetFromJsonAsync<AnalyticsResult>($"/api/analytics/overview?from={today.AddDays(-2):yyyy-MM-dd}&to={today:yyyy-MM-dd}&farmId={female.FarmId}", options);
        Assert.NotNull(result); Assert.Equal(1, result.Reproduction.EvaluatedFemales); Assert.Equal(100m, result.Reproduction.PregnancyPercent); Assert.Equal(100m, result.Reproduction.FertilityPercent);
        var none = await client.GetFromJsonAsync<AnalyticsResult>($"/api/analytics/overview?from={today.AddDays(-2):yyyy-MM-dd}&to={today:yyyy-MM-dd}&farmId={Guid.NewGuid()}", options);
        Assert.NotNull(none); Assert.Equal(0, none.Reproduction.ServedFemales); Assert.Null(none.Reproduction.PregnancyPercent);
    }
}
