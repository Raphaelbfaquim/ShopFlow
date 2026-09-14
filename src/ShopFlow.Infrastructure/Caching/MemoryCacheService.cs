using System.Collections.Concurrent;
using ShopFlow.Application.Abstractions.Caching;

namespace ShopFlow.Infrastructure.Caching;

public sealed class MemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _store = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(key, out var entry) && entry.ExpiresAtUtc > DateTime.UtcNow)
        {
            return Task.FromResult((T?)entry.Value);
        }

        _store.TryRemove(key, out _);
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _store[key] = new CacheEntry(value!, DateTime.UtcNow.Add(ttl));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private sealed record CacheEntry(object Value, DateTime ExpiresAtUtc);
}
