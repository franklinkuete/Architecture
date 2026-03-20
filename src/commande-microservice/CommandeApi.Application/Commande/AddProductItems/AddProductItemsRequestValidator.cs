using FluentValidation;

namespace CommandeApi.Application.Commande.AddProductItems;

public class AddProductItemsRequestValidator : AbstractValidator<AddProductItemsCommand>
{
    public AddProductItemsRequestValidator()
    {
        // Validation du conteneur principal
        RuleFor(x => x.Request.ProductItems)
            .NotEmpty().WithMessage("Vous devez rajouter une liste de produits");

        // Validation de chaque élément de la liste
        RuleForEach(x => x.Request.ProductItems).ChildRules(product =>
        {
            product.RuleFor(p => p.ProductName)
                .NotEmpty().WithMessage("Le nom du produit est obligatoire");

            product.RuleFor(p => p.ProductId)
                .NotEmpty().WithMessage("L'identifiant du produit est obligatoire");

            product.RuleFor(p => p.Qte)
                .GreaterThan(0).WithMessage("La quantité doit être supérieure à 0");

            product.RuleFor(p => p.Amount)
                .GreaterThanOrEqualTo(0).WithMessage("Le montant ne peut pas être négatif");
        });

        // Optionnel : Valider l'ID de commande si nécessaire
        RuleFor(x => x.Request.CommandeId)
            .NotNull().WithMessage("L'ID de la commande est requis");
    }
}
