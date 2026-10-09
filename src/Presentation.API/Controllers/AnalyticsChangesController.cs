using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Authorization;
using Presentation.API.Realtime;
namespace Presentation.API.Controllers;

[ApiController, Route("api/analytics"), Authorize(Roles = "Admin,Administrador")]
public sealed class AnalyticsChangesController(AnalyticsChanges changes) : ControllerBase
{
    [HttpGet("changes"), HasPermission("inventory.list"), HasPermission("products.list"),
     HasPermission("production.list"), HasPermission("weights.list"), HasPermission("reproduction.list"), HasPermission("animals.list")]
    public async Task Changes(CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache, no-store";
        Response.Headers["X-Accel-Buffering"] = "no";
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var seconds = 60d;
        if (long.TryParse(User.FindFirst("exp")?.Value, out var expiry))
            seconds = Math.Min(seconds, (DateTimeOffset.FromUnixTimeSeconds(expiry) - DateTimeOffset.UtcNow).TotalSeconds);
        lifetime.CancelAfter(TimeSpan.FromSeconds(Math.Max(0.01, seconds)));
        using var subscription = changes.Subscribe();
        try
        {
            await SendAsync("ready", "0", lifetime.Token);
            while (!lifetime.IsCancellationRequested)
            {
                using var heartbeat = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                heartbeat.CancelAfter(TimeSpan.FromSeconds(15));
                try
                {
                    var revision = await subscription.Reader.ReadAsync(heartbeat.Token);
                    await SendAsync("changed", revision.ToString(CultureInfo.InvariantCulture), lifetime.Token);
                }
                catch (OperationCanceledException) when (!lifetime.IsCancellationRequested)
                { await SendAsync("heartbeat", "0", lifetime.Token); }
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
    }
    private async Task SendAsync(string type, string value, CancellationToken ct)
    {
        await Response.WriteAsync($"event: {type}\ndata: {value}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
