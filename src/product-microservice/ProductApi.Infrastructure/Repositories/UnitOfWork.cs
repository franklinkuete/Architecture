using ProductApi.Domain.Interfaces;
using ProductApi.Infrastructure.Entities;

namespace ProductApi.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWorkProduct
{
    private ProductDbContext _dbContext;
    private readonly IProductRepository _productRepository;
    private readonly ICategorieRepository _categorieRepository;

    public IProductRepository ProductRepository => _productRepository;
    public ICategorieRepository CategorieRepository => _categorieRepository;

    public UnitOfWork(IProductRepository productRepository,
        ICategorieRepository categoryRepository,
        ProductDbContext dbContext)
    {
        _productRepository = productRepository;
        _categorieRepository = categoryRepository;
        _dbContext = dbContext;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}