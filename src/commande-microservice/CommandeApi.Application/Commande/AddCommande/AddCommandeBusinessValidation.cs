using Ardalis.Result;
using CommandeApi.Domain.Interfaces;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CommandeApi.Application.Commande.AddCommande;

public class AddCommandeBusinessValidation : IBusinessValidation<AddCommandeCommand, Result<CommandeResponse>>
{
    private readonly IUnitOfWorkCommande _unitOfWork;
    private readonly ILogger<AddCommandeBusinessValidation> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AddCommandeBusinessValidation(IUnitOfWorkCommande unitOfWork,
        ILogger<AddCommandeBusinessValidation> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<Result<CommandeResponse>> ValidateAsync(AddCommandeCommand request, CancellationToken cancellationToken)
    {
        // verifie que la liste des produits existe
        if (request.commande.ProductItems == null || !request.commande.ProductItems.Any())
        {
            _logger.LogWarning("AddCommandeBusinessValidation : La validation de la commande {commandeId} a échoué. La liste des produits est vide.", request.commande.Id);
            return Result<CommandeResponse>.Invalid(new ValidationError("La liste des produits ne peut pas être vide."));
        }

        foreach (var productItem in request.commande.ProductItems)
        {
            if (productItem.Qte <= 0)
            {
                var errorText = $"Un ou plusieurs des produits possède une quantité égale ou inférieur à O. (Commande : {request.commande.Id} Produit : {productItem.ProductName} Quantité : {productItem.Qte} TraceId : {_httpContextAccessor?.HttpContext?.TraceIdentifier})";

                _logger.LogError(errorText);
                return Result<CommandeResponse>.Invalid(new ValidationError("productItem.Qte", errorText));
            }
        }
        _logger.LogInformation("AddCommandeBusinessValidation : Validation métier de la commande {commandeId} réussie.", request.commande.Id);
        return Result<CommandeResponse>.Success(null!);
    }
}
