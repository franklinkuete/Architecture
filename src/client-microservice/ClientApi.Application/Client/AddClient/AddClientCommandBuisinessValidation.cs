using Ardalis.Result;
using ClientApi.Application.Client;
using ClientApi.Application.Client.AddClient;
using ClientApi.Domain.Interfaces;
using Core.Interfaces;
/// <summary>
/// Validateur spécifique pour la commande AddClientCommand.
/// Vérifie les règles métier complexes (ex: unicité) avant d'autoriser la création.
/// </summary>
public class AddClientCommandBusinessValidation
    : IBusinessValidation<AddClientCommand, Result<ClientResponse>>
{
    private readonly IUnitOfWorkClient _unitOfWork;

    // L'injection de dépendances permet d'accéder à la base de données pour les vérifications
    public AddClientCommandBusinessValidation(IUnitOfWorkClient unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Méthode de validation appelée par le Pipeline Behavior.
    /// </summary>
    public async Task<Result<ClientResponse>> ValidateAsync(AddClientCommand request, CancellationToken cancellationToken)
    {
        // 1. Appel au repository pour vérifier si un client identique existe déjà (Règle d'unicité)
        var exists = await _unitOfWork.ClientRepository.ClientExistsAsync(
            request.client.Firstname!,
            request.client.Lastname!,
            request.client.Datenaissance);

        // 2. Si un tel client existe, on bloque l'opération
        if (exists)
        {
            // On retourne un résultat "Invalid" avec un message clair.
            // Le Handler ne sera pas exécuté, protégeant ainsi l'intégrité des données.
            return Result<ClientResponse>.Invalid(new ValidationError(
                this.ToString(),
                $"Le client avec le nom {request.client.Lastname} et le prénom {request.client.Firstname} et la date de naissance {request.client.Datenaissance} existe déjà"
            ));
        }

        // 3. Si tout est correct, on retourne un succès. 
        // On envoi un succes de null, mais on peut aussi envoyé un success de new ClientResponse()
        // Le Pipeline comprend que la validation est passée et appelle le Handler.
        return Result<ClientResponse>.Success(null!);
    }
}


