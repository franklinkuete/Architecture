using Ardalis.Result;
using CommandeApi.Domain.Interfaces;
using Core.Interfaces;
using Mapster;

namespace CommandeApi.Application.Commande.DeleteProductItems;

public record DeleteProductItemsCommande(List<int> ItemIds) : ICommand<CommandeResponse>, ICacheInvalidator
{
    public List<string> CacheKeysToInvalidate => new List<string> {  $"{CommandeConst.GetAllCacheKeyPrefix}*" };
};
internal class DeleteProductItemsCommandHandler : ICommandHandler<DeleteProductItemsCommande, CommandeResponse>
{
    private readonly IUnitOfWorkCommande _unitOfWork;
    public DeleteProductItemsCommandHandler(IUnitOfWorkCommande unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CommandeResponse>> Handle(DeleteProductItemsCommande request, CancellationToken cancellationToken)
    {
        var resultat = await _unitOfWork.CommandeRepository
               .DeleteItems(request.ItemIds);

        if (resultat != null)
        {
            return Result.Success(resultat.Adapt<CommandeResponse>());
        }
        request.CacheKeysToInvalidate.Add($"{CommandeConst.ItemCacheKeyPrefix}-{resultat!.Id}");
        return Result.Invalid(new ValidationError("DeleteError", "Une erreur est survenue lors de la suppression des produits"));

    }
}
