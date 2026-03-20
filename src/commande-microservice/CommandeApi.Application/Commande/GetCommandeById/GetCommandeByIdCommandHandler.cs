using Ardalis.Result;
using CommandeApi.Domain.Interfaces;
using Mapster;

namespace CommandeApi.Application.Commande.GetCommandeById;

public record GetCommandeByIdQuery(int? CommandeId) : IQuery<CommandeResponse>, ICachedQuery<CommandeResponse>
{
    public string CacheKey => $"{CommandeConst.ItemCacheKeyPrefix}-{CommandeId}";
    public CachePolicy Policy => new CachePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(20));
};
internal class GetCommandeByIdCommandHandler : IQueryHandler<GetCommandeByIdQuery, CommandeResponse>
{
    private readonly IUnitOfWorkCommande _unitOfWork;
    public GetCommandeByIdCommandHandler(IUnitOfWorkCommande unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Result<CommandeResponse>> Handle(GetCommandeByIdQuery request, CancellationToken cancellationToken)
    {
        var resultat = await _unitOfWork.CommandeRepository.GetCommandeByIdAsync(request.CommandeId!.Value);

        if (resultat != null)
        {
            // Mapper la valeur vers ton DTO
            return Result.Success(resultat.Adapt<CommandeResponse>());
        }
      
            return Result.Invalid(new ValidationError($"La commande n'existe pas avec l'Id {request.CommandeId}"));
    }
}
