using FluentValidation;

namespace CommandeApi.Application.Commande.AddCommande;

public class AddCommandRequestValidator :AbstractValidator<AddCommandeCommand>
{
    public AddCommandRequestValidator()
    {
            RuleFor(x => x.commande.ClientId)
                .NotEmpty().WithMessage("L'Id du client n'est pas correct");
              
                RuleFor(x => x.commande.Libelle)
            .NotEmpty().WithMessage("Le Libelle de la commande est manquant");


                RuleFor(x => x.commande.Description)
              .NotEmpty().WithMessage("La description de la commande est manquant");

    }
}
