using Ardalis.Result;
using Core.Helpers;
using Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections;

public class CachePipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
{
    private readonly IHybridCacheService _cache;
    private readonly ILogger<CachePipelineBehavior<TRequest, TResponse>> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Optimisation : on extrait le type de la valeur contenue dans le Result (ex: UserDto)
    private static readonly Type _valueType = typeof(TResponse).GetGenericArguments()[0];

    public CachePipelineBehavior(
        IHybridCacheService cache,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CachePipelineBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
        var requestName = typeof(TRequest).Name;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = traceId,
            ["RequestName"] = requestName,
            ["CacheKey"] = request.CacheKey
        }))
        {
            _logger.LogInformation("{@prefix} 🔍 Tentative de récupération depuis le cache L1 (Mémoire) ou L2 (Redis via HybridCache) (Key: {CacheKey}, TraceId: {TraceId})",
                Constante.Prefix.CachePrefix, request.CacheKey, traceId);

            // 1. Tentative de récupération (L1 : mémoire locale / L2 : Redis via HybridCache)
            var cachedData = await _cache.GetAsync(request.CacheKey, _valueType, request.Policy.MemoryTtl);

            if (cachedData is not null)
            {
                _logger.LogInformation("{@prefix} ✅ Cache HIT pour {RequestName} (Key: {CacheKey}, TraceId: {TraceId})",
                    Constante.Prefix.CachePrefix, requestName, request.CacheKey, traceId);

                return ResultFactory<TResponse>.Success(cachedData);
            }

            _logger.LogInformation("{@prefix} ⚠️ Cache MISS pour {RequestName} (Key: {CacheKey}, TraceId: {TraceId})",
                Constante.Prefix.CachePrefix, requestName, request.CacheKey, traceId);

            // 2. Exécution réelle de la requête
            var response = await next();

            // 3. Mise en cache uniquement si succès métier
            if (response is Ardalis.Result.IResult result && result.IsOk())
            {
                // Reccupération de la valeur contenue dans le Result (ex: ProductResponse)
                var value = result.GetValue();

                if (value is IEnumerable list && !list.GetEnumerator().MoveNext())
                {
                    _logger.LogInformation("{@prefix} ℹ️ Résultat vide, pas de mise en cache (Key: {CacheKey}, TraceId: {TraceId})",
                        Constante.Prefix.CachePrefix, request.CacheKey, traceId);

                    return response;
                }

                await _cache.SetAsync(request.CacheKey, value, request.Policy, ct);

                _logger.LogInformation("{@prefix} 💾 Valeur mise en cache pour {RequestName} (Key: {CacheKey}, TTL: {Ttl}, TraceId: {TraceId})",
                    Constante.Prefix.CachePrefix, requestName, request.CacheKey, request.Policy.MemoryTtl, traceId);
            }

            return response;
        }
    }
}