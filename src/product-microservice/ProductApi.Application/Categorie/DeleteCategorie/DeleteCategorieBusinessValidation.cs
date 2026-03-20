using Ardalis.Result;
using Core.Interfaces;

namespace ProductApi.Application.Categorie.DeleteCategorie;

public class DeleteCategorieBusinessValidation : IBusinessValidation<DeleteCategorieCommand, Result<bool?>>
{
    private readonly IUnitOfWorkProduct _unitOfWork;
    public DeleteCategorieBusinessValidation(IUnitOfWorkProduct unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Result<bool?>> ValidateAsync(DeleteCategorieCommand request, CancellationToken cancellationToken)
    {
        string nameOfThis = this.ToString()!.Contains('.') ? this.ToString()!.Split('.').Last() : this.ToString()!;

        var categorie = await _unitOfWork.CategorieRepository.GetCategorieByIdAsync(request.id);

        if (categorie == null)
        {
            return Result<bool?>.Invalid(new ValidationError(nameOfThis,
               $"La catégorie avec l'Id {request.id} n'existe pas."));
        }

        var productExist = await _unitOfWork.ProductRepository.CategorieExistAsync(categorie.Id);
        if (productExist)
        {
            return Result<bool?>.Invalid(new ValidationError(nameOfThis,
              $"La Catégorie {categorie.CategorieName} est liée à un ou plusieurs produits"));
        }

        return Result<bool?>.Success(null);
    }
}
