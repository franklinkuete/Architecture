using Ardalis.Result;
using Mapster;
using ProductApi.Application.Product;
using ProductApi.Domain.Models;

namespace ProductApi.Application.Categorie.AddCategorie;

public sealed record AddCategorieCommand(string CategorieName) : ICommand<CategorieResponse>;
internal class AddCategorieCommandHandler : ICommandHandler<AddCategorieCommand, CategorieResponse>
{
    private readonly IUnitOfWorkProduct _unitOfWork;
    public AddCategorieCommandHandler(IUnitOfWorkProduct unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Result<CategorieResponse>> Handle(AddCategorieCommand request, CancellationToken cancellationToken)
    {

        if (string.IsNullOrEmpty(request.CategorieName))
        {
            return Result.Invalid(new ValidationError() { Identifier = "CategorieNotFound", ErrorMessage = "La catégorie doit être renseigné" });
        }
        // Mapper la requête vers le domaine
        var categorie = new CategoriePOCO() { CategorieName = request.CategorieName };

        // Appeler le repo
        var Categorie = await _unitOfWork.CategorieRepository
                .AddCategorieAsync(categorie);

        if (Categorie == null)
        {
            return Result.Invalid(new ValidationError() { Identifier = "CategorieNotFound", ErrorMessage = "La catégorie n'existe pas en base de données" });
        }

        return Result.Success(Categorie.Adapt<CategorieResponse>());
    }
}
