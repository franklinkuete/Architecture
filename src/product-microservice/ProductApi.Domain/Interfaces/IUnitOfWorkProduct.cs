
using ProductApi.Domain.Interfaces;

public interface IUnitOfWorkProduct
{
    IProductRepository ProductRepository { get; }
    ICategorieRepository CategorieRepository { get; }
}
