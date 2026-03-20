using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClientApi.Application.Client.AddClient;

public class AddClientRequestValidator : AbstractValidator<AddClientCommand>

{
   public AddClientRequestValidator()
    {
        RuleFor(x => x.client.Email)
           .EmailAddress().WithMessage("L'email du client n'est pas correct");

        RuleFor(x => x.client.Firstname)
           .NotEmpty().WithMessage("Le Prénom du client est manquant");

        RuleFor(x => x.client.Lastname)
           .NotEmpty().WithMessage("Le Nom du client est manquant");

    }
}
