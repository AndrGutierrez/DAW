using System.Collections.Concurrent;
using System.Threading.Channels;
namespace Presentation.API.Realtime;

public sealed class AnalyticsChanges
{
    private readonly ConcurrentDictionary<Guid, Channel<long>> subscribers = new();
    private long revision;
    public Subscription Subscribe()
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<long>(new BoundedChannelOptions(1)
        { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true, SingleWriter = false });
        subscribers[id] = channel;
        return new(channel.Reader, () => { subscribers.TryRemove(id, out _); channel.Writer.TryComplete(); });
    }
    public void Publish()
    {
        var value = Interlocked.Increment(ref revision);
        foreach (var channel in subscribers.Values) channel.Writer.TryWrite(value);
    }
    public sealed class Subscription(ChannelReader<long> reader, Action dispose) : IDisposable
    {
        public ChannelReader<long> Reader { get; } = reader;
        public void Dispose() => dispose();
    }
}

public sealed class AnalyticsChangeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AnalyticsChanges changes)
    {
        await next(context);
        if (context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.StartsWithSegments("/api/auth") &&
            (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) ||
             HttpMethods.IsPatch(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method)) &&
            context.Response.StatusCode is >= 200 and < 300)
            changes.Publish();
    }
}
