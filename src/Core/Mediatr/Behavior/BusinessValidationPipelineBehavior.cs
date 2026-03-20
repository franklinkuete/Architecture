using Ardalis.Result;
using Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.Mediatr.Behavior;

/// <summary>
/// Pipeline MediatR qui intercepte les requêtes pour appliquer les règles de validation métier.
/// Il ne s'exécute que pour les requêtes marquées avec 'IBusinessValidatable'.
/// </summary>
/// <typeparam name="TRequest">Le type de la commande ou requête entrante.</typeparam>
/// <typeparam name="TResponse">Le type de retour, qui doit impérativement implémenter IResult (Ardalis).</typeparam>
public class BusinessValidationPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IBusinessValidationMarker // Filtre : la requête doit être validable
    where TResponse : Ardalis.Result.IResult // Contrainte : le retour doit être un objet Result
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BusinessValidationPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BusinessValidationPipelineBehavior(
        IServiceProvider serviceProvider,
        ILogger<BusinessValidationPipelineBehavior<TRequest, TResponse>> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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
            // 1. Tente de récupérer dynamiquement le validateur spécifique à cette requête
            // On cherche une classe qui implémente IBusinessValidation<MaCommande, MonResultat>
            _logger.LogInformation("{@prefix} 🔍 Recherche d’un validateur pour {RequestName} (TraceId: {TraceId})",
                Constante.Prefix.BusinessValidationPrefix, requestName, traceId);

            var validator = _serviceProvider.GetService<IBusinessValidation<TRequest, TResponse>>();

            // 2. Si un validateur spécifique a été enregistré dans l'injection de dépendances
            if (validator != null)
            {
                _logger.LogInformation("{@prefix} ⚙️ Validateur trouvé pour {RequestName}, exécution de la validation métier (TraceId: {TraceId})",
                    Constante.Prefix.BusinessValidationPrefix, requestName, traceId);

                // Exécute la logique de validation métier
                var result = await validator.ValidateAsync(request, cancellationToken);

                // 3. Si la validation échoue (ex: doublon, règle métier violée, etc)
                if (!result.IsOk())
                {
                    _logger.LogWarning("{@prefix} ❌ Validation échouée pour {RequestName}. Erreurs: {Errors} (TraceId: {TraceId})",
                        Constante.Prefix.BusinessValidationPrefix, requestName,
                        string.Join(", ", result.ValidationErrors.Select(e => e.ErrorMessage)), traceId);

                    // On interrompt le pipeline et on retourne l'erreur immédiatement
                    // Le handler (AddClientCommandHandler par exemple) ne sera jamais appelé
                    return result;
                }

                _logger.LogInformation("{@prefix} ✔️ Validation réussie pour {RequestName} (TraceId: {TraceId})",
                    Constante.Prefix.BusinessValidationPrefix, requestName, traceId);
            }
            else
            {
                _logger.LogInformation("{@prefix} ℹ️ Aucun validateur trouvé pour {RequestName}, passage direct au handler (TraceId: {TraceId})",
                    Constante.Prefix.BusinessValidationPrefix, requestName, traceId);
            }

            // 4. Si pas de validateur trouvé ou si la validation est réussie, 
            // on passe à l'étape suivante du pipeline (ou au Handler final).
            var response = await next();

            _logger.LogInformation("{@prefix} 🏁 Exécution de {RequestName} terminée (TraceId: {TraceId})",
                Constante.Prefix.BusinessValidationPrefix, requestName, traceId);

            return response;
        }
    }
}
