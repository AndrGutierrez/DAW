using Microsoft.AspNetCore.Http;
using Presentation.API.Realtime;
namespace Core.Tests;
public sealed class AnalyticsChangeTests
{
    [Fact]
    public async Task NotificationReachesAllSubscribersWithoutBlockingOnSlowReaders()
    {
        var changes = new AnalyticsChanges(); using var first = changes.Subscribe(); using var second = changes.Subscribe();
        changes.Publish(); changes.Publish(); changes.Publish();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        Assert.Equal(3, await first.Reader.ReadAsync(timeout.Token)); Assert.Equal(3, await second.Reader.ReadAsync(timeout.Token));
        Assert.False(first.Reader.TryRead(out _));
    }
    [Theory]
    [InlineData("POST", "/api/animals", 201, true)] [InlineData("PUT", "/api/weights/1", 200, true)] [InlineData("DELETE", "/api/production/1", 204, true)]
    [InlineData("POST", "/api/animals", 400, false)] [InlineData("PUT", "/api/animals/1", 403, false)] [InlineData("GET", "/api/analytics/overview", 200, false)]
    [InlineData("POST", "/api/auth/login", 200, false)] [InlineData("POST", "/swagger", 200, false)]
    public async Task OnlySuccessfulResourceWritesPublishInvalidation(string method, string path, int status, bool expected)
    {
        var changes = new AnalyticsChanges(); using var subscription = changes.Subscribe(); var context = new DefaultHttpContext();
        context.Request.Method = method; context.Request.Path = path;
        await new AnalyticsChangeMiddleware(c => { c.Response.StatusCode = status; return Task.CompletedTask; }).InvokeAsync(context, changes);
        Assert.Equal(expected, subscription.Reader.TryRead(out _));
    }
}
