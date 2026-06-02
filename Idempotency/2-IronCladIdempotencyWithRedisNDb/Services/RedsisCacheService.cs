namespace AsbHybridIdempotencyDemo.Services;

public class RedisCacheService
{
    private readonly HashSet<string> _cache = new();

    public bool Exists(string key) => _cache.Contains(key);

    public void Set(string key)
    {
        _cache.Add(key);
        Console.WriteLine($"⚡ Redis SET: {key}");
    }

    public void SimulateEviction()
    {
        _cache.Clear();
        Console.WriteLine("⚠️ Redis cache evicted (simulated)");
    }
}