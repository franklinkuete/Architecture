using Ardalis.Result;
using Mapster;

namespace ProductApi.Application.Product.GetProductById;

public sealed record GetProductByIdQuery(Guid productId) : IQuery<ProductResponse>, ICachedQuery<ProductResponse>
{
    public string CacheKey => $"{ProductConst.ItemCacheKeyPrefix}-{productId}";
    public CachePolicy Policy => new CachePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(20));
};
internal class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    private readonly IUnitOfWorkProduct _unitOfWork;
    public GetProductByIdQueryHandler(IUnitOfWorkProduct unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var productIdGuid = request.productId;
        var product = await _unitOfWork.ProductRepository.GetProductByIdAsync(productIdGuid);

        if (product == null)
        {
            return Result.Invalid(new ValidationError("ProductNotFound", "Product not found"));
        }

        return Result.Success(product!.Adapt<ProductResponse>());
    }
}
