namespace Core.Application.Operations;
public sealed record BcvReference(DateOnly EffectiveDate, decimal BolivarsPerDollar, DateTime PublishedAt, string Source);
public interface IBcvRateProvider { Task<BcvReference> GetAsync(CancellationToken ct); }
public sealed record BcvSyncStatus(DateTime? LastAttempt, DateTime? LastSuccess, string? Error, bool AutomaticEnabled);
public interface IBcvSynchronizer
{
    BcvSyncStatus Status { get; }
    Task<ExchangeRateQuote> SyncAsync(CancellationToken ct);
}
public sealed class ExchangeRateUnavailableException(string message, Exception inner) : Exception(message, inner);
