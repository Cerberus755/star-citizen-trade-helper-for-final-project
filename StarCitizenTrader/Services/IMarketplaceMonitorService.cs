namespace StarCitizenTrader.Services;

/// Background service that polls the marketplace API and checks for wishlist matches.
public interface IMarketplaceMonitorService : IDisposable
{
    /// Fires when marketplace data has been refreshed.
    event EventHandler? DataRefreshed;

    /// Start background polling.
    void Start();

    /// Stop background polling.
    void Stop();

    /// Force an immediate refresh cycle.
    Task RefreshNowAsync(CancellationToken ct = default);

    /// Whether the monitor is currently running.
    bool IsRunning { get; }

    /// Last time data was successfully refreshed.
    DateTime? LastRefreshTime { get; }
}
