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
    private static readonly ConcurrentDictionary<string, int> _versions = new();
    private static readonly ConcurrentDictionary<string, HashSet<string>> _taggedKeys = new();

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
            return await _cache.GetOrCreateAsync<T>(
                versionedKey,
                async cancel => default!,
                cancellationToken: CancellationToken.None
            );
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
            var keysToRemove = _taggedKeys.Keys
                .Where(k => k.Contains(pattern.Replace("*", "")))
                .ToList();

            foreach (var key in keysToRemove)
            {
                IncrementVersion(key);
                await _cache.RemoveAsync(GetVersionedKey(key), CancellationToken.None);
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
                    set.Add(key);
                    return set;
                });
        }
    }
}
