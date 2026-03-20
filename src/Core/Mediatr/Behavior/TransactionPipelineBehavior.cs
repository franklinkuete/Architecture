using Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Core.Behavior;

public class TransactionPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly DbContext? _dbContext;
    private readonly ILogger<TransactionPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly ITransactionStatus _transactionStatus;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TransactionPipelineBehavior(
        IServiceProvider serviceProvider,
        ILogger<TransactionPipelineBehavior<TRequest, TResponse>> logger,
        ITransactionStatus transactionStatus,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = serviceProvider.GetService<DbContext>();
        _logger = logger;
        _transactionStatus = transactionStatus;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
        var requestName = typeof(TRequest).Name;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = traceId,
            ["RequestName"] = requestName
        }))
        {
            // 1. Les Queries passent sans transaction
            if (RequestClassifier.IsQuery(request!))
            {
                _logger.LogInformation("{@prefix} ℹ️ Requête {RequestName} identifiée comme Query → pas de transaction (TraceId: {TraceId})",
                    Constante.Prefix.DBPrefix, requestName, traceId);

                return await next();
            }

            // 2. Cas EF Core
            if (_dbContext != null && !RequestClassifier.IsQuery(request!))
            {
                _logger.LogInformation("{@prefix} ⚙️ Requête {RequestName} identifiée comme Command → transaction EF démarrée (TraceId: {TraceId})",
                    Constante.Prefix.DBPrefix, requestName, traceId);

                return await HandleEfTransaction(requestName, next, cancellationToken, traceId);
            }

            // 3. Fallback : on passe au suivant sans transaction
            _logger.LogError("{@prefix} ⚠️ Aucun DbContext disponible pour {RequestName} → exécution sans transaction (TraceId: {TraceId})",
                Constante.Prefix.DBPrefix, requestName, traceId);

            return await next();
        }
    }

    // Méthode privée EF Core
    private async Task<TResponse> HandleEfTransaction(
        string requestName,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken,
        string traceId)
    {
        var strategy = _dbContext!.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var response = await next();

                if (response is Ardalis.Result.IResult result && !result.ValidationErrors.Any())
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("{@prefix} ✅ Transaction EF validée sur {RequestName} (TraceId: {TraceId})",
                        Constante.Prefix.DBPrefix, requestName, traceId);

                    // --- EXÉCUTION DES ACTIONS POST-COMMIT ---
                    foreach (var action in _transactionStatus.PostCommitActions)
                    {
                        try
                        {
                            await action(cancellationToken);
                            _logger.LogInformation("{@prefix} 📌 Action post-commit exécutée avec succès pour {RequestName} (TraceId: {TraceId})",
                                Constante.Prefix.DBPrefix, requestName, traceId);
                        }
                        catch (Exception ex)
                        {
                            // On ne throw pas ici pour éviter de retourner une 500 
                            // alors que la DB a été mise à jour avec succès.
                            _logger.LogError(ex, "{@prefix} ❌ Erreur lors d'une action post-commit sur {RequestName} (TraceId: {TraceId})",
                                Constante.Prefix.DBPrefix, requestName, traceId);
                        }
                    }
                }
                else
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError("{@prefix} ❌ Transaction EF annulée sur {RequestName} (TraceId: {TraceId})",
                        Constante.Prefix.DBPrefix, requestName, traceId);
                }

                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "{@prefix} ❌ Transaction EF annulée sur {RequestName} avec exception (TraceId: {TraceId})",
                    Constante.Prefix.DBPrefix, requestName, traceId);
                throw;
            }
        });
    }
}
public static class RequestClassifier
{
    // Cache pour savoir si un type correspond à une Query.
    // La clé est le Type de la requête, la valeur est un bool indiquant si c'est une Query.
    private static readonly ConcurrentDictionary<Type, bool> _isQueryCache = new();

    // Cache pour savoir si un type correspond à une Command.
    // La clé est le Type de la requête, la valeur est un bool indiquant si c'est une Command.
    private static readonly ConcurrentDictionary<Type, bool> _isCommandCache = new();

    public static bool IsCommand(object request)
    {
        // On récupère le type de l'objet passé en paramètre.
        var type = request.GetType();

        // On utilise le cache pour éviter de recalculer à chaque appel.
        // Si le type n'est pas encore dans le cache, on exécute la réflexion une seule fois :
        // - On parcourt les interfaces implémentées par le type.
        // - On vérifie si l'une d'elles correspond à ICommand<T>.
        // Le résultat est ensuite stocké dans le cache pour les appels futurs.
        return _isCommandCache.GetOrAdd(type, t =>
            t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>))
        );
    }

    public static bool IsQuery(object request)
    {
        // Même logique que pour IsCommand, mais appliquée à IQuery<T>.
        var type = request.GetType();

        // Le cache évite de refaire la réflexion pour chaque requête du même type.
        return _isQueryCache.GetOrAdd(type, t =>
            t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>))
        );
    }
}




