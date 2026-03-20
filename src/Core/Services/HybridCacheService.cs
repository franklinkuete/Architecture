using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace Core.Services;
/// <summary>
/// Service de cache hybride combinant la rapidité de la mémoire locale (L1) 
/// et la persistance partagée de Redis (L2).
/// </summary>
public class HybridCacheService : IHybridCacheService
{
    private readonly IMemoryCache _memoryCache;               // Cache en mémoire locale (L1) : rapide mais limité à l’instance
    private readonly IDistributedCache _distributedCache;     // Cache distribué (L2, Redis) : partagé entre toutes les instances
    private readonly IConnectionMultiplexer _redisConnection; // Connexion Redis directe pour Pub/Sub et SCAN
    private readonly JsonSerializerOptions _jsonOptions;      // Options de sérialisation JSON (camelCase, pas d’indentation)
    private readonly ILogger<HybridCacheService> _logger;     // Logger pour tracer les opérations

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(); // Verrous par clé pour éviter le "cache stampede"
    private const string InvalidationChannel = "cache:invalidation";                    // Canal Redis Pub/Sub pour synchroniser les invalidations
    private static readonly ConcurrentDictionary<string, bool> _localKeys = new();      // Registre des clés présentes en L1 pour invalidation ciblée
    private readonly IHttpContextAccessor _contextAccessor;                             // accesseur du context http
    private readonly string? _traceId;
    public HybridCacheService(IMemoryCache memoryCache,
                              IDistributedCache distributedCache,
                              IConnectionMultiplexer redisConnection,
                              ILogger<HybridCacheService> logger,
                              IHttpContextAccessor contextAccessor)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _redisConnection = redisConnection;
        _logger = logger;
        _contextAccessor = contextAccessor;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Sérialisation en camelCase
            WriteIndented = false                              // Pas d’indentation pour réduire la taille
        };

        _traceId = Activity.Current?.TraceId.ToString()
             // 2. Sinon, on se rabat sur le HTTP (si on est dans un controller)
             ?? _contextAccessor.HttpContext?.TraceIdentifier
             // 3. Sinon, on met "N/A" ou "Background"
             ?? "Background-Task";

        // Souscription au canal Redis pour écouter les invalidations envoyées par d’autres instances
        var sub = _redisConnection.GetSubscriber();
        sub.Subscribe(RedisChannel.Literal(InvalidationChannel), (channel, message) =>
        {
            string msg = message.ToString();
            if (string.IsNullOrEmpty(msg)) return; // Sécurité : ignore les messages vides

            if (msg.StartsWith("prefix:"))
            {
                // Cas invalidation par préfixe : on extrait le préfixe réel
                string actualPrefix = msg.Substring("prefix:".Length);
                InvalidateLocalByPrefix(actualPrefix); // Invalidation ciblée en L1
            }
            else
            {
                InvalidateLocalKey(msg); // Invalidation d’une clé unique en L1
            }
        });
    }

    // Invalide une clé spécifique en L1
    private void InvalidateLocalKey(string key)
    {
        _memoryCache.Remove(key);              // Supprime la clé du cache local
        _localKeys.TryRemove(key, out _);      // Supprime la clé du registre interne
        _logger.LogDebug("{cachePrefix} L1 invalidé pour la clé : {key}, TraceId : {trace}", Constante.Prefix.CachePrefix, key, _traceId);
    }

    // Invalide toutes les clés locales correspondant à un préfixe
    private void InvalidateLocalByPrefix(string prefix)
    {
        var keysToRemove = _localKeys.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var key in keysToRemove)
        {
            InvalidateLocalKey(key); // Supprime chaque clé correspondante
        }

        // CORRECTION : Passage en LogDebug pour éviter la pollution visuelle des logs
        _logger.LogDebug("{cachePrefix} Invalidation L1 terminée pour le préfixe : {prefix} ({count} clés, TraceId : {traceId}", Constante.Prefix.CachePrefix, prefix, keysToRemove.Count, _traceId);
    }

    // Lecture : L1 -> L2 -> Miss
    public async Task<object?> GetAsync(string cacheKey, Type type, TimeSpan? slidingExpiration, CancellationToken ct)
    {
        if (_memoryCache.TryGetValue(cacheKey, out object? value))
        {
            _logger.LogDebug("{cachePrefix} ✔️ Valeur reccupérée du cache L1 Rapide, pour la clé {key}, TraceId {traceId}", Constante.Prefix.CachePrefix, cacheKey, _traceId);
            return value; // Retour immédiat si trouvé en L1
        }

        var redisData = await _distributedCache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(redisData))
        {
            var deserialized = JsonSerializer.Deserialize(redisData, type, _jsonOptions);
            if (deserialized != null)
            {
                // Utilise le TTL passé en paramètre au lieu de 5 min fixes
                var ttl = slidingExpiration ?? TimeSpan.FromMinutes(5);

                _logger.LogDebug("{cachePrefix} Valeur propagée en L1 pour la clé {key}, TraceId { traceId}", Constante.Prefix.CachePrefix, cacheKey, _traceId);
                AddToLocalCache(cacheKey, deserialized, ttl); // Propagation vers L1

                _logger.LogDebug("{cachePrefix} ✔️ Valeur reccupérée du cache L2 Redis pour la clé {key}, TraceId {traceId}", Constante.Prefix.CachePrefix, cacheKey, _traceId);
                return deserialized;
            }
        }
        return null; // Cache miss : donnée absente partout
    }

    // Écriture : stocke en L1 et L2
    public async Task SetAsync(string cacheKey, object? valueToCache, CachePolicy cachePolicy, CancellationToken ct)
    {
        var myLock = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await myLock.WaitAsync(ct);

        try
        {
            var memoryTtl = cachePolicy.MemoryTtl ?? TimeSpan.FromMinutes(5);
            AddToLocalCache(cacheKey, valueToCache, memoryTtl); // Stockage en L1

            var serialized = JsonSerializer.Serialize(valueToCache, _jsonOptions);
            await _distributedCache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cachePolicy.RedisTtl
            }, ct); // Stockage en L2 (Redis)
        }
        finally
        {
            myLock.Release();                       // Libère le verrou
            _locks.TryRemove(cacheKey, out _);      // Supprime le verrou du dictionnaire pour libérer la mémoire
        }
    }

    // Ajoute une clé en L1 avec TTL et callback d’éviction
    private void AddToLocalCache(string key, object? value, TimeSpan ttl)
    {
        _memoryCache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl, // Durée de vie en L1
            PostEvictionCallbacks =               // Callback déclenché à l’expiration
            {
                new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (k, v, reason, state) => _localKeys.TryRemove(k.ToString()!, out _)
                }
            }
        });
        _localKeys[key] = true; // Enregistre la clé dans le registre interne
    }

    // Suppression d’une clé en L1 et L2 + notification aux autres instances
    public async Task RemoveAsync(string cacheKey, CancellationToken ct)
    {
        InvalidateLocalKey(cacheKey); // Supprime en L1
        await _distributedCache.RemoveAsync(cacheKey, ct); // Supprime en L2 (Redis)

        // Notification aux autres instances via Pub/Sub
        await _redisConnection.GetDatabase().PublishAsync(RedisChannel.Literal(InvalidationChannel), cacheKey);

        _logger.LogDebug("{cachePrefix} ❌ Cache supprimé en L1 et L2 pour la clé {key}, TraceId {traceId}", Constante.Prefix.CachePrefix, cacheKey, _traceId);
    }

    // Invalidation par préfixe (L1 et L2)
    public async Task InvalidateByPrefixAsync(string prefix, CancellationToken ct)
    {
        // 1. Suppression groupée dans Redis (L2) - Plus rapide qu'un foreach
        var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().First());
        var pattern = $"*{prefix}*";
        var keys = server.Keys(pattern: pattern).ToArray();

        if (keys.Length > 0)
        {
            await _redisConnection.GetDatabase().KeyDeleteAsync(keys); // Suppression atomique
        }

        // 2. Notification globale (Pub/Sub)
        // IMPORTANT : On ne nettoie pas le L1 local ici manuellement. 
        // On laisse le Subscriber (ligne 38) s'en charger pour TOUTES les instances, 
        // y compris celle-ci. Cela évite les doublons de logs et de traitements.

        await _redisConnection.GetDatabase().PublishAsync(
            RedisChannel.Literal(InvalidationChannel),
            $"prefix:{prefix}");

        _logger.LogInformation("{cachePrefix} Ordre d'invalidation globale envoyé pour le préfixe : {prefix}, TraceId {traceId}", Constante.Prefix.CachePrefix, prefix, _traceId);
    }
}
