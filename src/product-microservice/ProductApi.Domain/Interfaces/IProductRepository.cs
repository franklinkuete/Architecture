
using Core.Events;
using ProductApi.Domain.Models;

public interface IProductRepository
{
    Task<ProductPOCO?> GetProductByIdAsync(Guid id);
    Task<IEnumerable<ProductPOCO>> GetAllProductAsync();
    Task<bool> CategorieExistAsync(int idCategorie);
    Task<IEnumerable<ProductPOCO>> GetAllProductAsync(int pageIndex = 0, int pageSize = 50);
    Task<ProductPOCO> AddProductAsync(ProductPOCO Product);
    Task<ProductPOCO?> UpdateProductAsync(ProductPOCO Product);
    Task<bool> RemoveProductAsync(Guid Id);
    Task<List<string>> UpdateStock(CommandeCreatedEvent request);
    Task<List<ProductPOCO?>> UpdateStock(CommandeCancelEvent request);
}
// normalement la couche Domain ne devrait pas référencer Ardalis.Result, mais pour simplifier on peut faire une exception ici. Sinon il faudrait créer nos propres types de résultat dans le domaine et faire le mapping dans les handlers.
// c'est une approche pragmatique pour éviter de créer une couche supplémentaire de types de résultat dans le domaine, surtout si on utilise déjà Ardalis.Result dans toute l'application.
// on sacrifie un peu la séparation stricte des couches pour gagner en simplicité et éviter le boilerplate de mapping entre les types de résultat du domaine et ceux de l'application.