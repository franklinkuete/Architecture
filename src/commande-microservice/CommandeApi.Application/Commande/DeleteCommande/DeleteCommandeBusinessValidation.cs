using Ardalis.Result;
using CommandeApi.Application.Commande.AddCommande;
using CommandeApi.Domain.Interfaces;
using Core.Interfaces;

namespace CommandeApi.Application.Commande.DeleteCommande;

public class DeleteCommandeBusinessValidation : IBusinessValidation<DeleteCommandeCommand, Result<bool>>
{
    private readonly IUnitOfWorkCommande _unitOfWork;
    public DeleteCommandeBusinessValidation(IUnitOfWorkCommande unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Result<bool>> ValidateAsync(DeleteCommandeCommand request, CancellationToken cancellationToken)
    {
        var response = await _unitOfWork.CommandeRepository.GetCommandeByIdAsync(request.CommandeId);

        if (response == null)
        {
            return Result<bool>.Invalid(new ValidationError("La commande à supprimer n'existe pas"));
        }

        return Result<bool>.Success(false);
    }
}
