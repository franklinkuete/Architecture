using Ardalis.Result;
using CommandeApi.Domain.Interfaces;
using Mapster;

namespace CommandeApi.Application.Commande.GetAllCommandes;

public record GetAllCommandesQuery(int pageIndex = 0, int pageSize = int.MaxValue) : IQuery<List<CommandeResponse>>, ICachedQuery<List<CommandeResponse>>
{
    public string CacheKey => $"{CommandeConst.GetAllCacheKeyPrefix}:page={pageIndex}:size={pageSize}";
    public CachePolicy Policy => new CachePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));
}
internal class GetAllCommandesQueryHandler : IQueryHandler<GetAllCommandesQuery, List<CommandeResponse>>
{
    private readonly IUnitOfWorkCommande _unitOfWork;
    public GetAllCommandesQueryHandler(IUnitOfWorkCommande unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<CommandeResponse>>> Handle(GetAllCommandesQuery request, CancellationToken cancellationToken)
    {
        var queryResult = await _unitOfWork.CommandeRepository
                    .GetAllCommandeAsync(request.pageIndex, request.pageSize);

        return Result.Success(queryResult.Adapt<List<CommandeResponse>>());
    }
}
