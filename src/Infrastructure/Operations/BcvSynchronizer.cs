using Core.Application.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace Infrastructure.Operations;
public sealed class BcvSyncOptions { public bool Enabled { get; set; } }
public sealed class BcvSynchronizer(IServiceScopeFactory scopes, IOptions<BcvSyncOptions> options) : IBcvSynchronizer
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private DateTime? attempt, success; private string? error;
    public BcvSyncStatus Status => new(attempt, success, error, options.Value.Enabled);
    public async Task<ExchangeRateQuote> SyncAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            attempt = DateTime.UtcNow;
            using var scope = scopes.CreateScope();
            BcvReference reference;
            try { reference = await scope.ServiceProvider.GetRequiredService<IBcvRateProvider>().GetAsync(ct); }
            catch (Exception failure) when (failure is HttpRequestException or InvalidDataException or System.Text.Json.JsonException or InvalidOperationException or FormatException or OverflowException or TaskCanceledException)
            {
                if (ct.IsCancellationRequested) throw;
                error = "Automatic BCV retrieval is unavailable. The saved reference is retained; retry or enter a manual reference.";
                throw new ExchangeRateUnavailableException(error, failure);
            }
            var quote = await scope.ServiceProvider.GetRequiredService<ExchangeRateService>().RecordAutomaticAsync(reference, ct);
            success = DateTime.UtcNow; error = null; return quote;
        }
        finally { gate.Release(); }
    }
}
public sealed class BcvRefreshService(IBcvSynchronizer synchronizer, IOptions<BcvSyncOptions> options, ILogger<BcvRefreshService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        do
        {
            try { await synchronizer.SyncAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception failure) { logger.LogWarning(failure, "BCV synchronization failed; previously stored references are retained."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
