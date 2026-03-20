using Ardalis.Result;
using CommandeApi.Domain.Interfaces;
using Mapster;

namespace CommandeApi.Application.Commande.GetAllCommandesByClientId;

public record GetAllCommandesQueryByClient(string clientId, int pageIndex = 0, int pageSize = int.MaxValue) : IQuery<List<CommandeResponse>>, ICachedQuery<List<CommandeResponse>>
{
    public string CacheKey => $"{CommandeConst.GetAllCacheKeyPrefix}:page={pageIndex}:size={pageSize}-{clientId}";
    public CachePolicy Policy => new CachePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));
};
internal class GetAllCommandesByClientQueryHandler : IQueryHandler<GetAllCommandesQueryByClient, List<CommandeResponse>>
{
    private readonly IUnitOfWorkCommande _unitOfWork;
    public GetAllCommandesByClientQueryHandler(IUnitOfWorkCommande unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Result<List<CommandeResponse>>> Handle(GetAllCommandesQueryByClient request, CancellationToken cancellationToken)
    {
        var query = await _unitOfWork.CommandeRepository
            .GetAllCommandeByClientIdAsync(request.clientId, request.pageIndex, request.pageSize);

        return Result.Success(query.Adapt<List<CommandeResponse>>());
    }
}
