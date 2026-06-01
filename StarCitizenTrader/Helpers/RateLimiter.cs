using System.Collections.Concurrent;

namespace StarCitizenTrader.Helpers;

/// Token-bucket rate limiter that enforces the UEX API limit of 120 requests/minute.
public class RateLimiter : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConcurrentQueue<DateTime> _requestTimestamps = new();
    private readonly int _maxRequests;
    private readonly TimeSpan _window;

    public RateLimiter(int maxRequestsPerMinute = 120)
    {
        _maxRequests = maxRequestsPerMinute;
        _window = TimeSpan.FromMinutes(1);
    }

    /// Waits until a request slot is available within the rate limit window.
    public async Task WaitForSlotAsync(CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            // Remove expired timestamps
            var cutoff = DateTime.UtcNow - _window;
            while (_requestTimestamps.TryPeek(out var oldest) && oldest < cutoff)
                _requestTimestamps.TryDequeue(out _);

            // If at capacity, wait until the oldest request expires
            if (_requestTimestamps.Count >= _maxRequests)
            {
                if (_requestTimestamps.TryPeek(out var oldestTs))
                {
                    var delay = oldestTs.Add(_window) - DateTime.UtcNow;
                    if (delay > TimeSpan.Zero)
                        await Task.Delay(delay, ct);
                }
                // Clean again
                cutoff = DateTime.UtcNow - _window;
                while (_requestTimestamps.TryPeek(out var ts) && ts < cutoff)
                    _requestTimestamps.TryDequeue(out _);
            }

            _requestTimestamps.Enqueue(DateTime.UtcNow);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public int CurrentCount => _requestTimestamps.Count;

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
