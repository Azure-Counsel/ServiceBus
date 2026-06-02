namespace AsbIdempotencyDemo.Services;

public class IdempotencyStore
{
    private readonly Dictionary<string, DateTime> _cache = new();
    private readonly TimeSpan _window = TimeSpan.FromMinutes(10);

    public bool IsDuplicate(string messageId, DateTime now)
    {
        Cleanup(now);

        if (_cache.ContainsKey(messageId))
            return true;

        _cache[messageId] = now;
        return false;
    }

    private void Cleanup(DateTime now)
    {
        var expired = _cache
            .Where(x => now - x.Value > _window)
            .Select(x => x.Key)
            .ToList();

        foreach (var key in expired)
        {
            _cache.Remove(key);
            Console.WriteLine($"🧹 Expired: {key}");
        }
    }
}