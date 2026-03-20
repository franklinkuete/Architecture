using Ardalis.Result;
using MediatR;

namespace Core.Interfaces;

// Nouvelle interface typée
/// <summary>
/// Interface générique définissant le contrat pour la validation des règles métier.
/// Elle permet d'isoler la logique de vérification (ex: unicité, droits spécifiques) avant l'exécution d'une commande.
/// </summary>
/// <typeparam name="in TRequest">Le type de la requête (Command) à valider. Le mot-clé 'in' indique la contravariance.</typeparam>
/// <typeparam name="TResponse">Le type de résultat attendu, qui doit implémenter IResult (Ardalis).</typeparam>
public interface IBusinessValidation<in TRequest, TResponse>
    where TRequest : IRequest<TResponse> // Contrainte : TRequest doit être une requête MediatR retournant TResponse
    where TResponse : IResult             // Contrainte : TResponse doit être un objet de type Résultat (Ardalis)
{
    /// <summary>
    /// Exécute de manière asynchrone la validation des règles métier pour la requête donnée.
    /// </summary>
    /// <param name="request">L'instance de la requête à valider.</param>
    /// <param name="cancellationToken">Jeton d'annulation pour stopper l'opération si nécessaire.</param>
    /// <returns>
    /// Retourne un TResponse (Result). 
    /// Si le résultat est 'Success', le pipeline continue. 
    /// S'il est 'Invalid' ou 'Error', le pipeline s'arrête et retourne l'erreur.
    /// </returns>
    Task<TResponse> ValidateAsync(TRequest request, CancellationToken cancellationToken);
}
