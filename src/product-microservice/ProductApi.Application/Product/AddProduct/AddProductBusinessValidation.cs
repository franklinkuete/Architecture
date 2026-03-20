using Ardalis.Result;
using Core.Interfaces;

namespace ProductApi.Application.Product.AddProduct;
/// <summary>
/// Validateur métier pour la création d'un produit.
/// Vérifie spécifiquement l'existence des références externes (clés étrangères).
/// </summary>
public class AddProductBusinessValidation : IBusinessValidation<AddProductCommand, Result<ProductResponse>>
{
    private readonly IUnitOfWorkProduct _unitOfWork;

    public AddProductBusinessValidation(IUnitOfWorkProduct unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Vérifie si les données liées au produit (comme la catégorie) sont valides en base de données.
    /// </summary>
    public async Task<Result<ProductResponse>> ValidateAsync(AddProductCommand request, CancellationToken cancellationToken)
    {
        // Nom de la classe courante (utilisé pour tracer l'origine de l'erreur)
        string nameOfThis = this.ToString()!;

        // 1. Récupération de l'identifiant de la catégorie depuis la requête
        var idCategorie = request.ProductRequest.IdCategorie;

        if (idCategorie.HasValue)
        {
            // 2. Vérification de l'existence de la catégorie dans le référentiel
            // Règle métier : un produit doit obligatoirement appartenir à une catégorie existante.
            var categorie = await _unitOfWork.CategorieRepository.GetCategorieByIdAsync(idCategorie.Value);

            // 3. Si la catégorie est trouvée, la validation est considérée comme réussie
            if (categorie != null)
            {
                // Retourne un succès (le produit peut continuer son processus de création dans le Handler)
                return Result<ProductResponse>.Success(null!);

                // ⚠️ Remarque : le PipelineBehavior interceptera ce "Success" et permettra
                // l'exécution du Handler associé à la commande.
            }
        }

        // 4. Si la catégorie n'existe pas, on retourne une erreur de validation
        return Result<ProductResponse>.Invalid(new ValidationError(
                     // Extraction du nom de la classe pour identifier la source de l'erreur
                     nameOfThis.Contains('.') ? nameOfThis.Split('.').Last() : nameOfThis,
                     // Message explicite indiquant la cause de l'erreur
                     $"La catégorie avec l'Id {idCategorie} n'existe pas."
                 ));
    }
}
