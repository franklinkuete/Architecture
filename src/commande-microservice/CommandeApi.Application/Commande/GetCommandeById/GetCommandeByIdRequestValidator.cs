using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommandeApi.Application.Commande.GetCommandeById
{
    public class GetCommandeByIdRequestValidator : AbstractValidator<GetCommandeByIdQuery>
    {
        public GetCommandeByIdRequestValidator()
        {
            RuleFor(x => x.CommandeId)
                .NotEmpty().WithMessage("L'Id de la commande doit être renseigné'");
        }
    }
}
