using FluentValidation;

namespace ProductApi.Application.Product.AddProduct;

public class AddProductRequestValidator : AbstractValidator<AddProductCommand>
{
    public AddProductRequestValidator()
    {
        RuleFor(x => x.ProductRequest.Name)
            .NotEmpty().WithMessage("Le nom du produit est obligatoire")
            .MaximumLength(200).WithMessage("Le nom du produit ne peut pas dépasser 200 caractères");

        RuleFor(x => x.ProductRequest.Description)
            .NotEmpty().WithMessage("La description du produit est obligatoire");

        RuleFor(x => x.ProductRequest.Quantity)
        .GreaterThan(0).WithMessage("La quantité doit être supérieure à 0");

        RuleFor(x => x.ProductRequest.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Le Montant doit être supérieur ou égal à 0");

        RuleFor(x => x.ProductRequest.IdCategorie)
            .GreaterThan(0).WithMessage("L'identifiant de la catégorie doit être supérieur à 0")
            .NotNull().WithMessage("L'identifiant de la catégorie est obligatoire");
    }
}
