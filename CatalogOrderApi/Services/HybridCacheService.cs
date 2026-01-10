using Microsoft.Extensions.Caching.Hybrid;
using System.Collections.Concurrent;

namespace CatalogOrderApi.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
}

public class HybridCacheService : ICacheService
{
    private readonly HybridCache _cache;
    private readonly ILogger<HybridCacheService> _logger;
    private readonly ConcurrentDictionary<string, int> _versions = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _taggedKeys = new();
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private DateTime _lastCleanup = DateTime.UtcNow;

    public HybridCacheService(HybridCache cache, ILogger<HybridCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var versionedKey = GetVersionedKey(key);
            
            // Try to get cached value, return default if not found
            var result = await _cache.GetOrCreateAsync<T?>(
                versionedKey,
                async cancel => default,
                cancellationToken: CancellationToken.None
            );
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var versionedKey = GetVersionedKey(key);
            var options = new HybridCacheEntryOptions
            {
                Expiration = expiry ?? TimeSpan.FromMinutes(30),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            };

            await _cache.SetAsync(versionedKey, value, options, cancellationToken: CancellationToken.None);
            
            // Track the key for pattern-based invalidation
            TrackKey(key);
            
            // Periodic cleanup
            PerformCleanupIfNeeded();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            // Increment version to invalidate all versioned keys
            IncrementVersion(key);
            await _cache.RemoveAsync(GetVersionedKey(key), CancellationToken.None);
            
            // Remove from tracking
            RemoveFromTracking(key);
            
            _logger.LogInformation("Invalidated cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var patternWithoutWildcard = pattern.Replace("*", "");
            var keysToRemove = _taggedKeys.Keys
                .Where(k => k.StartsWith(patternWithoutWildcard, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                IncrementVersion(key);
                await _cache.RemoveAsync(GetVersionedKey(key), CancellationToken.None);
                RemoveFromTracking(key);
            }

            _logger.LogInformation("Invalidated {Count} cache keys matching pattern: {Pattern}", keysToRemove.Count, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
        }
    }

    private string GetVersionedKey(string key)
    {
        var version = _versions.GetOrAdd(key, 0);
        return $"{key}:v{version}";
    }

    private void IncrementVersion(string key)
    {
        _versions.AddOrUpdate(key, 1, (k, v) => v + 1);
    }

    private void TrackKey(string key)
    {
        // Extract tag from key (e.g., "items:all" -> "items")
        var parts = key.Split(':');
        if (parts.Length > 0)
        {
            var tag = parts[0];
            _taggedKeys.AddOrUpdate(
                tag,
                new HashSet<string> { key },
                (t, set) =>
                {
                    lock (set)
                    {
                        set.Add(key);
                    }
                    return set;
                });
        }
    }

    private void RemoveFromTracking(string key)
    {
        var parts = key.Split(':');
        if (parts.Length > 0)
        {
            var tag = parts[0];
            if (_taggedKeys.TryGetValue(tag, out var keys))
            {
                lock (keys)
                {
                    keys.Remove(key);
                    if (keys.Count == 0)
                    {
                        _taggedKeys.TryRemove(tag, out _);
                    }
                }
            }
        }
    }

    private void PerformCleanupIfNeeded()
    {
        if (DateTime.UtcNow - _lastCleanup > CleanupInterval)
        {
            _lastCleanup = DateTime.UtcNow;
            // Cleanup old versions that are no longer needed
            var keysToClean = _versions.Where(kvp => kvp.Value > 5).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToClean)
            {
                if (_versions.TryGetValue(key, out var version) && version > 5)
                {
                    _versions.TryUpdate(key, 0, version); // Reset to 0
                }
            }
            _logger.LogInformation("Performed cache cleanup, reset {Count} version counters", keysToClean.Count);
        }
    }
}
