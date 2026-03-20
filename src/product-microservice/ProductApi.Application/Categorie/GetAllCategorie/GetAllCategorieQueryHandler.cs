using Ardalis.Result;
using Mapster;
using ProductApi.Application.Product;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProductApi.Application.Categorie.GetAllCategorie;

public sealed record GetAllCategorieCommand : IQuery<IEnumerable<CategorieResponse>>;
internal class GetAllCategorieQueryHandler : IQueryHandler<GetAllCategorieCommand, IEnumerable<CategorieResponse>>
{
    private readonly IUnitOfWorkProduct _unitOfWork;
    public GetAllCategorieQueryHandler(IUnitOfWorkProduct unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<CategorieResponse>>> Handle(GetAllCategorieCommand request, CancellationToken cancellationToken)
    {
        var categorieList = await _unitOfWork.CategorieRepository
                 .GetAllCategorietAsync();

        return Result.Success(categorieList.Adapt<IEnumerable<CategorieResponse>>());
    }
}
