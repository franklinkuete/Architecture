using Ardalis.Result;
using ClientApi.Domain.Interfaces;
using Core.Interfaces;
using Mapster;

namespace ClientApi.Application.Client.GetById;

public record GetByIdQuery(int Id) : IQuery<ClientResponse>,ICachedQuery<ClientResponse>
{
    public string CacheKey => $"{ClientConst.ItemCacheKeyPrefix}-{Id}";
    public CachePolicy Policy => new CachePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(20));
};
public class GetByIdQueryHandler : IQueryHandler<GetByIdQuery, ClientResponse>
{
    private readonly IUnitOfWorkClient _unitOfWork;
    public GetByIdQueryHandler(IUnitOfWorkClient unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Result<ClientResponse>> Handle(GetByIdQuery request, CancellationToken cancellationToken)
    {
        var clientResult = await _unitOfWork.ClientRepository.GetClientByIdAsync(request.Id);

        if (clientResult!=null)
        {
            return Result.Success(clientResult.Adapt<ClientResponse>());
        }
       
        return Result.Invalid(new ValidationError("ClientNotFound", $"Le client (Id = {request.Id}) recherché est introuvable"));
    }
}
