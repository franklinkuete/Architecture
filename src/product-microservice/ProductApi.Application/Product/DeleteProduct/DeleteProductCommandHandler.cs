using Ardalis.Result;
using Core.Interfaces;

namespace ProductApi.Application.Product.DeleteProduct
{
    public sealed record DeleteProductCommand(Guid id) : ICommand<bool>, ICacheInvalidator
    {
        public List<string> CacheKeysToInvalidate => new List<string>()
        {
            $"{ProductConst.GetAllCacheKeyPrefix}*",
            $"{ProductConst.ItemCacheKeyPrefix}-{id}"
        };
    }

    internal class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, bool>
    {
        private readonly IUnitOfWorkProduct _unitOfWork;
        public DeleteProductCommandHandler(IUnitOfWorkProduct unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<Result<bool>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var resultat = await _unitOfWork.ProductRepository.RemoveProductAsync(request.id);
            return Result.Success(resultat);
        }
    }
}
