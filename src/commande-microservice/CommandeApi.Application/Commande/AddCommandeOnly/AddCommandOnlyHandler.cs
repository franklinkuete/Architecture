using Ardalis.Result;
using CommandeApi.Domain.Interfaces;
using CommandeApi.Domain.Models;
using Core.Interfaces;
using Mapster;

namespace CommandeApi.Application.Commande.AddCommandeOnly;

public record AddCommandeCommandOnly(CommandeRequest commande) : ICommand<CommandeResponse>, ICacheInvalidator        // Marqueur pour déclencher l'invalidation automatique du cache (via un Pipeline Behavior)

{
    public List<string> CacheKeysToInvalidate => new List<string> { $"{CommandeConst.GetAllCacheKeyPrefix}*" };
}
internal class AddCommandOnlyHandler : ICommandHandler<AddCommandeCommandOnly, CommandeResponse>
{
    private readonly IUnitOfWorkCommande _unitOfWork;
    public AddCommandOnlyHandler(IUnitOfWorkCommande unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Result<CommandeResponse>> Handle(AddCommandeCommandOnly request, CancellationToken cancellationToken)
    {
        // 1. Mapping du DTO de requête vers le modèle de domaine "Commande"
        // On transforme les données reçues de l'API en objet métier manipulable
        var newCommande = request.commande.Adapt<CommandeApi.Domain.Models.Commande>();

        // 2. Initialisation des propriétés par défaut de la nouvelle commande
        newCommande.ProductItems = [];               // On s'assure que la liste des produits est vide au départ
        newCommande.Statut = StatutCommande.Initial; // La commande commence toujours en état "initial"
        newCommande.Date = DateTime.UtcNow;          // Horodatage universel (UTC) pour la traçabilité

        // 3. Persistance de la commande via le Repository (Pattern Unit of Work)
        // Cette étape insère la commande en base de données et retourne l'entité créée (avec son ID généré)
        var entity = await _unitOfWork.CommandeRepository
            .AddCommandeAsync(newCommande);

        // 4. Gestion d'erreur si la création échoue
        if (entity == null)
        {
            // On retourne un échec de validation si le repository n'a pas pu sauvegarder l'objet
            Result.Invalid(new ValidationError("NewCommandeError", "La commande n'a pas pu être créée"));
        }

        // 5. Préparation de la réponse
        // On re-mappe l'entité créée (qui contient maintenant son ID de base de données) vers le DTO de réponse
        var newOrder = entity.Adapt<CommandeResponse>();

        // 6. Retour du succès avec l'objet finalisé
        return Result.Success(newOrder!);

    }
}
