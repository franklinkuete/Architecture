using Ardalis.Result;
using ClientApi.Domain.Interfaces;
using Mapster;
using MediatR;

namespace ClientApi.Application.Client.GetAll;

public  record GetAllClientQuery(int pageIndex=0, int pageSize=int.MaxValue) : ICachedQuery<IEnumerable<ClientResponse>>
{
    public string CacheKey => $"{ClientConst.GetAllCacheKeyPrefix}:page={pageIndex}:size={pageSize}";
    public CachePolicy Policy => new CachePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15));
}
public class GetAllClientQueryHandler : IQueryHandler<GetAllClientQuery, IEnumerable<ClientResponse>>
{
    private readonly IUnitOfWorkClient _unitOfWork;
    public GetAllClientQueryHandler(IUnitOfWorkClient unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<ClientResponse>>> Handle(GetAllClientQuery request, CancellationToken cancellationToken)
    {

        var clients = await _unitOfWork.ClientRepository.
            GetAllClientAsync(request.pageIndex,request.pageSize);

        return Result.Success(clients.Adapt<IEnumerable<ClientResponse>>());
    }
}
