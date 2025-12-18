using System.Collections.Concurrent;

namespace TaskManager.RateLimiting;

public class InMemoryRateLimitStore // In-memory store with thread safety. In a real-world application, consider using a distributed store like Redis for scalability.
{
    private readonly ConcurrentDictionary<string, RateLimitEntry> _store = new();

    public bool TryConsume(string key, RateLimitOptions options, out int remaining)
    {
        var now = DateTime.UtcNow;

        var entry = _store.GetOrAdd(key, _=> new RateLimitEntry
        {
            PermitCount = 0,
            WindowStart = now
        });

        lock(entry)
        {
            if(now - entry.WindowStart > options.Window)
            {
                entry.PermitCount = 0;
                entry.WindowStart = now;
            }

            if(entry.PermitCount >= options.PermitLimit)
            {
                remaining = 0;
                return false;
            }

            entry.PermitCount++;
            remaining = options.PermitLimit - entry.PermitCount;
            return true;
        }
    }
}