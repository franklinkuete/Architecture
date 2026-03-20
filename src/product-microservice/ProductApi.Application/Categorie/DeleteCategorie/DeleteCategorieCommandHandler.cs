using Ardalis.Result;
using Core.Interfaces;

namespace ProductApi.Application.Categorie.DeleteCategorie;

public sealed record DeleteCategorieCommand(int id) : ICommand<bool?>, IBusinessValidationMarker;
internal class DeleteCategorieCommandHandler : ICommandHandler<DeleteCategorieCommand, bool?>
{
    private readonly IUnitOfWorkProduct _unitOfWork;
    public DeleteCategorieCommandHandler(IUnitOfWorkProduct unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool?>> Handle(DeleteCategorieCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.CategorieRepository.RemoveCategorieAsync(request.id);
    }
}
