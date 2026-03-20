using Ardalis.Result;
using CommandeApi.Application.Commande.AddCommande;
using CommandeApi.Domain.Interfaces;
using Core.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CommandeApi.Application.Commande.AddProductItems;

public class AddProductItemsBuisinessValidation : IBusinessValidation<AddProductItemsCommand, Result<CommandeResponse>>
{
    private readonly IUnitOfWorkCommande _unitOfWork;
    private readonly ILogger<AddCommandeBusinessValidation> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public AddProductItemsBuisinessValidation(IUnitOfWorkCommande unitOfWork,
        ILogger<AddCommandeBusinessValidation> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<Result<CommandeResponse>> ValidateAsync(AddProductItemsCommand request, CancellationToken cancellationToken)
    {
        var commandeId = request.Request.CommandeId;

        // 1. Vérification de la présence de l'ID (Safety check)
        if (!commandeId.HasValue)
        {
            return Result<CommandeResponse>.Invalid(new ValidationError
            {
                Identifier = nameof(request.Request.CommandeId),
                ErrorMessage = "L'identifiant de la commande est requis."
            });
        }

        // 2. Appel asynchrone 
        var commande = await _unitOfWork.CommandeRepository.GetCommandeByIdAsync(commandeId.Value);

        // 3. Gestion du cas "Non trouvé" (Standard Ardalis)
        if (commande is null)
        {

            return Result<CommandeResponse>.Invalid(new ValidationError
            {
                Identifier = "CommandeId",
                ErrorMessage = $"La commande avec l'id {commandeId} n'existe pas.",
                ErrorCode = "CommandeNotFound"
            });

        }
        foreach (var productItem in request.Request.ProductItems)
        {
            if (productItem.Qte <= 0)
            {
                var errorText = $"Un ou plusieurs des produits possède une quantité égale ou inférieur à O. (Commande : {request.Request.CommandeId} Produit : {productItem.ProductName} Quantité : {productItem.Qte} TraceId : {_httpContextAccessor?.HttpContext?.TraceIdentifier})";

                _logger.LogError(errorText);
                return Result<CommandeResponse>.Invalid(new ValidationError("productItem.Qte", errorText));
            }
           
        }

        // Si tout est ok, on continue le flux...
        return Result<CommandeResponse>.Success(commande.Adapt<CommandeResponse>());
    }

}
