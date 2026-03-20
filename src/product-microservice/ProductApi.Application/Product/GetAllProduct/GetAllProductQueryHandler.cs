

using Ardalis.Result;
using Mapster;

namespace ProductApi.Application.Product.GetAllProduct;

public sealed record GetAllProductCommand(int pageIndex = 0, int pageSize = int.MaxValue)
    : IQuery<IEnumerable<ProductResponse>>, ICachedQuery<IEnumerable<ProductResponse>>
{
    public string CacheKey => $"{ProductConst.GetAllCacheKeyPrefix}:page={pageIndex}:size={pageSize}";

    public CachePolicy Policy => new CachePolicy(
        MemoryTtl: TimeSpan.FromMinutes(5),   // TTL cache mémoire (rapide)
        RedisTtl: TimeSpan.FromMinutes(60)    // TTL cache distribué (Redis)
    );
}
public class GetAllProductQueryHandler : IQueryHandler<GetAllProductCommand, IEnumerable<ProductResponse>>
{
    private readonly IUnitOfWorkProduct _unitOfWork;
    public GetAllProductQueryHandler(IUnitOfWorkProduct unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<ProductResponse>>> Handle(GetAllProductCommand request, CancellationToken cancellationToken)
    {
        var result = await _unitOfWork.ProductRepository
            .GetAllProductAsync(request.pageIndex,request.pageSize);

        return Result.Success(result.Adapt<IEnumerable<ProductResponse>>());
    }
}
