using Microsoft.Extensions.Caching.Memory;

namespace server.Services;

public sealed class AppCacheService(IMemoryCache cache)
{
    public bool TryGetValue<T>(string key, out T? value)
    {
        return cache.TryGetValue(key, out value);
    }

    public void Set<T>(string key, T value)
    {
        cache.Set(key, value);
    }

    public void Set<T>(string key, T value, TimeSpan absoluteExpirationRelativeToNow)
    {
        cache.Set(key, value, absoluteExpirationRelativeToNow);
    }

    public void Remove(string key)
    {
        cache.Remove(key);
    }

    public void Clear()
    {
        if (cache is not MemoryCache memoryCache)
        {
            throw new InvalidOperationException("Aplikace nepouziva podporovanou memory cache.");
        }

        memoryCache.Compact(1.0);
    }
}
