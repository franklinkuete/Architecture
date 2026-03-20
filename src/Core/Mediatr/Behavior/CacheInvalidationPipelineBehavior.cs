using Ardalis.Result;
using Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Core.Mediatr.Behavior;
/// <summary>
/// Pipeline MediatR gérant l'invalidation automatique du cache après une commande (Write).
/// </summary>
/// <typeparam name="TRequest">La commande doit implémenter ICacheInvalidator pour fournir les clés à supprimer.</typeparam>
public class CacheInvalidationPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheInvalidator
{
    private readonly IHybridCacheService _cache;
    private readonly ILogger<CacheInvalidationPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly ITransactionStatus _transactionStatus;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CacheInvalidationPipelineBehavior(
        IHybridCacheService cache,
        ILogger<CacheInvalidationPipelineBehavior<TRequest, TResponse>> logger,
        IHttpContextAccessor httpContextAccessor,
        ITransactionStatus transactionStatus)
    {
        _cache = cache;
        _logger = logger;
        _transactionStatus = transactionStatus;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
        var requestName = typeof(TRequest).Name;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = traceId,
            ["RequestName"] = requestName
        }))
        {
            // Exécution du handler suivant dans le pipeline (la commande ou la requête réelle)
            _logger.LogInformation("{@prefix} 🚀 Exécution de {RequestName} démarrée (TraceId: {TraceId})",
                Constante.Prefix.CachePrefix, requestName, traceId);

            var response = await next();

            // Vérification : La réponse est un succès (IResult.IsOk) ET la requête contient des clés de cache à supprimer
            if (response is Ardalis.Result.IResult result && result.IsOk() && request.CacheKeysToInvalidate?.Any() == true)
            {
                _logger.LogInformation("{@prefix} 🧹 Invalidation programmée pour {RequestName} (Keys: {Keys}, TraceId: {TraceId})",
                    Constante.Prefix.CachePrefix, requestName, string.Join(", ", request.CacheKeysToInvalidate), traceId);

                // On enregistre une action à exécuter APRÈS la confirmation de la transaction (Post-Commit)
                // Cela évite d'invalider le cache si la transaction en base de données échoue finalement
                _transactionStatus.PostCommitActions.Add(async (ct) =>
                {
                    foreach (var keyOrPrefix in request.CacheKeysToInvalidate)
                    {
                        // Gestion de l'invalidation par pattern (ex: "get-allcommande*")
                        if (keyOrPrefix.EndsWith("*"))
                        {
                            _logger.LogInformation("{@prefix} 🗑️ Suppression par préfixe {Prefix} (TraceId: {TraceId})",
                                Constante.Prefix.CachePrefix, keyOrPrefix.TrimEnd('*'), traceId);

                            // Supprime toutes les entrées commençant par le préfixe
                            await _cache.InvalidateByPrefixAsync(keyOrPrefix.TrimEnd('*'), ct);
                        }
                        else
                        {
                            _logger.LogInformation("{@prefix} 🗑️ Suppression de la clé {Key} (TraceId: {TraceId})",
                                Constante.Prefix.CachePrefix, keyOrPrefix, traceId);

                            // Suppression d'une entrée unique et précise par sa clé
                            await _cache.RemoveAsync(keyOrPrefix, ct);
                        }
                    }
                });
            }

            // Retourne la réponse initiale au client
            _logger.LogInformation("{@prefix} ✔️ Exécution de {RequestName} terminée (TraceId: {TraceId})",
                Constante.Prefix.CachePrefix, requestName, traceId);

            return response;
        }
    }
}
