using System.Net;
using System.Text;
using Core.Application.Operations;
using Infrastructure.Operations;
namespace Core.Tests;
public sealed class BcvRateProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);
    private sealed class FixedClock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromResult(response(request)); }
    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static BcvRateProvider Provider(Handler handler) => new(new HttpClient(handler) { BaseAddress = new("https://bcv.today/") }, new FixedClock());
    [Fact]
    public async Task DatedReferencePreservesPrecisionEffectiveDateAndPublicationSource()
    {
        var paths = new List<string>();
        var provider = Provider(new(r => { paths.Add(r.RequestUri!.AbsolutePath); return Json("{\"USD\":874.7321,\"effective_date\":\"2026-10-08\",\"updated_at\":\"2026-10-07T22:34:35+00:00\"}"); }));
        var result = await provider.GetAsync(default); Assert.Equal(874.7321m, result.BolivarsPerDollar); Assert.Equal(new DateOnly(2026,10,8), result.EffectiveDate);
        Assert.Equal("https://bcv.today/api/v1/history/2026-10-08.json", result.Source); Assert.Single(paths);
    }
    [Fact]
    public async Task MissingTodaysFileFallsBackToLatestAndAcceptsThePreviousBusinessDay()
    {
        var paths = new List<string>(); var provider = Provider(new(r => { var path = r.RequestUri!.AbsolutePath; paths.Add(path); return path.Contains("history") ? new(HttpStatusCode.NotFound) : Json("{\"USD\":873.123456,\"effective_date\":\"2026-10-07\",\"updated_at\":\"2026-10-07T22:00:00Z\"}"); }));
        var result = await provider.GetAsync(default); Assert.Equal(2, paths.Count); Assert.EndsWith("/api/v1/rate.json", result.Source); Assert.Equal(new DateOnly(2026,10,7), result.EffectiveDate);
    }
    [Theory]
    [InlineData("0", "2026-10-08", "2026-10-07T22:00:00Z")]
    [InlineData("-1", "2026-10-08", "2026-10-07T22:00:00Z")]
    [InlineData("1.1234567", "2026-10-08", "2026-10-07T22:00:00Z")]
    [InlineData("800", "2026-10-09", "2026-10-07T22:00:00Z")]
    [InlineData("800", "2026-09-30", "2026-10-07T22:00:00Z")]
    [InlineData("800", "2026-10-08", "2026-09-30T22:00:00Z")]
    [InlineData("800", "2026-10-08", "2026-10-09T22:00:00Z")]
    public async Task InvalidStaleOrFutureDataCannotProduceAReference(string value, string day, string published)
    {
        var json = "{\"USD\":" + value + ",\"effective_date\":\"" + day + "\",\"updated_at\":\"" + published + "\"}";
        await Assert.ThrowsAsync<InvalidDataException>(() => Provider(new(_ => Json(json))).GetAsync(default));
    }
    [Fact]
    public async Task ProviderFailureDoesNotMasqueradeAsMissingData()
    { await Assert.ThrowsAsync<HttpRequestException>(() => Provider(new(_ => new(HttpStatusCode.ServiceUnavailable))).GetAsync(default)); }
}
