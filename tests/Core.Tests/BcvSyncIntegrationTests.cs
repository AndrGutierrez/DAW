using System.Net;
using System.Net.Http.Json;
using Core.Application.Operations;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace Core.Tests;
public sealed partial class ManagementIntegrationTests
{
    [Fact]
    public async Task AutomaticReferenceIsAuditedDeduplicatedAndRetainedWhenTheProviderFails()
    {
        var provider = new StubBcvProvider(); using var factory = Factory().WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<IBcvRateProvider>(); s.AddSingleton<IBcvRateProvider>(provider); }));
        await Seed(factory); using var client = factory.CreateClient(); await Login(client, "admin");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/exchange-rates/usd-ves/sync", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/exchange-rates/usd-ves/sync", null)).StatusCode);
        var saved = await client.GetFromJsonAsync<ExchangeRateQuote>("/api/exchange-rates/usd-ves"); Assert.Equal(874.7321m, saved!.BolivarsPerDollar); Assert.Equal("automatic", saved.EntryMethod);
        provider.Fail = true; var failure = await client.PostAsync("/api/exchange-rates/usd-ves/sync", null); Assert.Equal(HttpStatusCode.ServiceUnavailable, failure.StatusCode); Assert.Equal("application/problem+json", failure.Content.Headers.ContentType!.MediaType);
        var retained = await client.GetFromJsonAsync<ExchangeRateQuote>("/api/exchange-rates/usd-ves"); Assert.Equal(saved.Id, retained!.Id);
        var status = await client.GetFromJsonAsync<BcvSyncStatus>("/api/exchange-rates/usd-ves/status"); Assert.NotNull(status!.LastSuccess); Assert.NotNull(status.Error);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); Assert.Equal(1, await db.ExchangeRates.CountAsync()); Assert.True(await db.AuditLogs.AnyAsync(a => a.EntityId == saved.Id.ToString()));
        await Login(client, "employee"); Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/exchange-rates/usd-ves/sync", null)).StatusCode);
    }
    private sealed class StubBcvProvider : IBcvRateProvider
    {
        public bool Fail { get; set; }
        public Task<BcvReference> GetAsync(CancellationToken ct) => Fail ? throw new HttpRequestException("Unavailable") : Task.FromResult(new BcvReference(DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-4)), 874.7321m, DateTime.UtcNow, "https://bcv.today/api/v1/rate.json"));
    }
}
