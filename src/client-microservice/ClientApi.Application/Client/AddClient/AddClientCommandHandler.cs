using Ardalis.Result;
using ClientApi.Domain.Interfaces;
using Core.Interfaces;
using Mapster;

namespace ClientApi.Application.Client.AddClient;

/// <summary>
/// Définition de la commande pour ajouter un client.
/// Utilise un 'record' pour garantir l'immuabilité des données.
/// </summary>
/// <param name="client">Le DTO contenant les informations du client à créer.</param>
public record AddClientCommand(ClientRequest client)
    : ICommand<ClientResponse>, // Définit cette classe comme une commande MediatR retournant un ProductResponse
      ICacheInvalidator,          // Marqueur pour déclencher l'invalidation automatique du cache (via un Pipeline Behavior)
      IBusinessValidationMarker       // Marqueur indiquant que cette commande nécessite une validation des règles métier
{
    // Liste des entrées de cache à supprimer si la commande réussit (ex: rafraîchir la liste globale des clients)
    public List<string> CacheKeysToInvalidate => new List<string> { $"{ClientConst.GetAllCacheKeyPrefix}*" };
};

/// <summary>
/// Handler responsable de l'exécution de la logique d'ajout d'un client.
/// Marqué 'sealed' pour empêcher l'héritage et optimiser les performances.
/// </summary>
public sealed class AddClientCommandHandler : ICommandHandler<AddClientCommand, ClientResponse>
{
    private readonly IUnitOfWorkClient _unitOfWork;

    public AddClientCommandHandler(IUnitOfWorkClient unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ClientResponse>> Handle(AddClientCommand request, CancellationToken cancellationToken)
    {
        // 1. Récupération des données du client depuis la requête
        var dto = request.client;

        // 2. Mapping automatique du DTO vers l'entité du domaine (POCO) via Mapster
        var client = dto.Adapt<ClientApi.Domain.Entities.ClientPOCO>();

        // 3. Appel au repository pour l'insertion en base de données via l'Unit of Work
        var resultat = await _unitOfWork.ClientRepository.AddClientAsync(client);

        // 4. Vérification de la réussite de l'opération
        if (resultat != null)
        {
            // Retourne un succès enveloppé dans un objet 'Result' avec le DTO de réponse rempli
            //$"Le client {resultat.Firstname} {resultat.Lastname} a été créé avec succès",
            return Result.Success(resultat.Adapt<ClientResponse>());
        }

        // 5. En cas d'échec, retourne un résultat d'erreur typé pour informer la couche API
        return Result.Invalid(new ValidationError(
            "ClientCreation",
            $"Une erreur est survenue lors de la création du client {dto.Lastname} {dto.Firstname}")
        );
    }
}
