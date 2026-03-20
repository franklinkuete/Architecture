namespace Core.Interfaces;

// Interface pour un service de cache hybride combinant cache en mémoire et cache distribué (ex: Redis).
public interface IHybridCacheService
{
    Task<object?> GetAsync(string key, Type valueType, TimeSpan? slidingExpiration, CancellationToken ct = default);
    Task SetAsync(string key, object? value, CachePolicy policy, CancellationToken ct = default);
    Task RemoveAsync(string cacheKey, CancellationToken ct = default);
    Task InvalidateByPrefixAsync(string prefix, CancellationToken ct = default);
}

