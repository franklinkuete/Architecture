using Ardalis.Result;
using Core.Interfaces;

namespace ProductApi.Application.Product.UpdateProduct;
// Classe de validation métier pour la commande UpdateProductCommand.
// Elle implémente une interface générique IBusinessValidation qui retourne un Result<ProductResponse>.
public class UpdateProductBusinessValidator : IBusinessValidation<UpdateProductCommand, Result<ProductResponse>>
{
    private readonly IUnitOfWorkProduct _unitOfWork;

    // Le validateur reçoit une instance de UnitOfWork pour accéder aux repositories (produits, catégories).
    public UpdateProductBusinessValidator(IUnitOfWorkProduct unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Méthode principale de validation asynchrone.
    // Elle prend la commande UpdateProductCommand et retourne un Result<ProductResponse>.
    public async Task<Result<ProductResponse>> ValidateAsync(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        // On récupère le DTO (données envoyées par la commande).
        var dto = request.request;

        // On détermine le nom de la classe courante (utile pour tracer l'origine des erreurs).
        string nameOfThis = this.ToString()!.Contains('.') ? this.ToString()!.Split('.').Last() : this.ToString()!;

        // Vérification que l'Id du produit est bien renseigné et valide (>0).
        if (!dto.Id.HasValue)
        {
            return Result<ProductResponse>.Invalid(
                new ValidationError(nameOfThis, $"L'Id du produit n'est pas renseigné.")
            );
        }

        // Vérification que la catégorie existe si un IdCategorie est fourni.
        if (dto.IdCategorie.HasValue)
        {
            var categorie = _unitOfWork.CategorieRepository.GetCategorieByIdAsync(dto.IdCategorie!.Value);
            if (categorie == null)
            {
                return Result<ProductResponse>.Invalid(
                    new ValidationError(nameOfThis, $"La catégorie avec l'Id {dto.IdCategorie} n'existe pas")
                );
            }
        }

        // Vérification que le produit existe bien en base.
        var product = await _unitOfWork.ProductRepository.GetProductByIdAsync(dto.Id.Value);
        if (product == null)
        {
            return Result<ProductResponse>.Invalid(
                new ValidationError(nameOfThis, $"Le produit avec l'Id {dto.Id.Value} n'existe pas")
            );
        }

        // Si toutes les validations passent, on retourne un succès.
        // Ici, la valeur retournée est null car la validation ne construit pas encore de ProductResponse.
        return Result<ProductResponse>.Success(null!);
    }
}
