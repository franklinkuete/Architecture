using Ardalis.Result;
using CommandeApi.Domain.Interfaces;
using Core.Interfaces;
using Mapster;

namespace CommandeApi.Application.Commande.DeleteCommande;

public record DeleteCommandeCommand(int CommandeId) : ICommand<bool>,ICacheInvalidator, IBusinessValidationMarker
{
    public List<string> CacheKeysToInvalidate => new List<string> { 
        $"{CommandeConst.ItemCacheKeyPrefix}-{CommandeId}",
        $"{CommandeConst.GetAllCacheKeyPrefix}*" };
};
internal class DeleteCommandeHandler : ICommandHandler<DeleteCommandeCommand, bool>
{
    private readonly IUnitOfWorkCommande _unitOfWork;
    public DeleteCommandeHandler(IUnitOfWorkCommande unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Result<bool>> Handle(DeleteCommandeCommand request, CancellationToken cancellationToken)
    {
        return Result.Success(await _unitOfWork.CommandeRepository
              .DeleteCommandeAsync(request.CommandeId));
    }
}
