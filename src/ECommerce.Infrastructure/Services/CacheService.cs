using System.Collections.Concurrent;
using System.Text.Json;
using ECommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace ECommerce.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ConcurrentDictionary<string, bool> _keys = new();
    private static readonly JsonSerializerOptions JsonOptions = new();

    public CacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public T? Get<T>(string key)
    {
        var bytes = _cache.Get(key);
        if (bytes is null || bytes.Length == 0) return default;
        return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(key, cancellationToken);
        if (bytes is null || bytes.Length == 0) return default;
        return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        var options = new DistributedCacheEntryOptions();
        options.AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(30);

        await _cache.SetAsync(key, bytes, options, cancellationToken);
        _keys.TryAdd(key, true);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
        _keys.TryRemove(key, out _);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var keysToRemove = _keys.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var key in keysToRemove)
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _keys.TryRemove(key, out _);
        }
    }
}
