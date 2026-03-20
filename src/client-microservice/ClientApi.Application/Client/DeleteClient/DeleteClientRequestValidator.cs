using ClientApi.Application.Client.AddClient;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClientApi.Application.Client.DeleteClient
{
    public class DeleteClientRequestValidator : AbstractValidator<DeleteClientCommand>
    {
        public DeleteClientRequestValidator()
        {
            RuleFor(x => x.id)
           .NotEmpty().WithMessage("L'Id du client doit être renseigné'");
        }
    }
}
