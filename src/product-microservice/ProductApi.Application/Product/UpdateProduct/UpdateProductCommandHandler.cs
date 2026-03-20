using Ardalis.Result;
using Core.Interfaces;
using Mapster;
using ProductApi.Domain.Models;

namespace ProductApi.Application.Product.UpdateProduct;

public sealed record UpdateProductCommand(UpdateProductRequest request) : ICommand<ProductResponse>, ICacheInvalidator, IBusinessValidationMarker
{
    public List<string> CacheKeysToInvalidate => new List<string>() {
       $"{ProductConst.GetAllCacheKeyPrefix}*",
          $"{ProductConst.ItemCacheKeyPrefix}-{request.Id}"
    };
}

internal class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, ProductResponse>
{
    private readonly IUnitOfWorkProduct _unitOfWork;
    public UpdateProductCommandHandler(IUnitOfWorkProduct unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductResponse>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        // Mapper la commande vers ton POCO
        var product = request.request.Adapt<ProductPOCO>();

        // Le repository retourne directement une entité
        var updatedProduct = await _unitOfWork.ProductRepository.UpdateProductAsync(product);

        if (updatedProduct == null)
        {
            // Cas métier : produit introuvable ou non mis à jour
            return Result.Invalid(new ValidationError("ProductNotFound", "produit introuvable ou non mis à jour"));
        }

        // Mapper l’entité vers ton DTO de réponse
        var response = updatedProduct.Adapt<ProductResponse>();

        return Result.Success(response);
    }
}
