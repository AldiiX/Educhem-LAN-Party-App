using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace server.Services;

public sealed class AppCacheService(IMemoryCache cache)
{
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public bool TryGetValue<T>(string key, out T? value)
    {
        return cache.TryGetValue(key, out value);
    }

    public void Set<T>(string key, T value)
    {
        _keys.TryAdd(key, 0);
        cache.Set(key, value);
    }

    public void Set<T>(string key, T value, TimeSpan absoluteExpirationRelativeToNow)
    {
        _keys.TryAdd(key, 0);
        cache.Set(key, value, absoluteExpirationRelativeToNow);
    }

    public void Remove(string key)
    {
        _keys.TryRemove(key, out _);
        cache.Remove(key);
    }

    public AppCacheClearResult Clear()
    {
        var keys = _keys.Keys.ToArray();
        var before = CaptureMemory(keys.Length);

        foreach (var key in keys)
        {
            cache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        if (cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var after = CaptureMemory(_keys.Count, compactManagedHeap: true);

        return new AppCacheClearResult(
            keys.Length,
            before.TrackedKeys,
            after.TrackedKeys,
            before.ManagedMemoryBytes,
            after.ManagedMemoryBytes
        );
    }

    private static CacheMemorySnapshot CaptureMemory(int trackedKeys, bool compactManagedHeap = false)
    {
        return new CacheMemorySnapshot(
            trackedKeys,
            GC.GetTotalMemory(compactManagedHeap)
        );
    }
}

public sealed record AppCacheClearResult(
    int RemovedKeys,
    int TrackedKeysBefore,
    int TrackedKeysAfter,
    long ManagedMemoryBeforeBytes,
    long ManagedMemoryAfterBytes
);

internal sealed record CacheMemorySnapshot(
    int TrackedKeys,
    long ManagedMemoryBytes
);
