using Ardalis.Result;
using Core.Interfaces;
using Mapster;
using ProductApi.Domain.Models;

namespace ProductApi.Application.Product.AddProduct;

public sealed record AddProductCommand(AddProductRequest ProductRequest)
    : ICommand<ProductResponse>, ICacheInvalidator, IBusinessValidationMarker
{
    // Invalider toutes les clés commençant par "GetAllProduct"
    public List<string> CacheKeysToInvalidate => new List<string>() 
    {
        $"{ProductConst.GetAllCacheKeyPrefix}*" 
    };
}
internal sealed class AddProductCommandHandler : ICommandHandler<AddProductCommand, ProductResponse>
{
    private readonly IUnitOfWorkProduct _unitOfWork;

    public AddProductCommandHandler(IUnitOfWorkProduct unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Result<ProductResponse>> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        // Mapper la requête vers le domaine
        var product = request.ProductRequest.Adapt<ProductPOCO>();

        // Appeler le repo
        var result = await _unitOfWork.ProductRepository
                .AddProductAsync(product);

        
        return Result.Success(result.Adapt<ProductResponse>());
    }

}
