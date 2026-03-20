using Ardalis.Result;
using ProductApi.Domain.Models;

namespace ProductApi.Domain.Interfaces;

public interface ICategorieRepository
{
    Task<IEnumerable<CategoriePOCO>> GetAllCategorietAsync();
    Task<CategoriePOCO> AddCategorieAsync(CategoriePOCO categorie);
    Task<bool> RemoveCategorieAsync(int Id);
    Task<CategoriePOCO?> GetCategorieByIdAsync(int Id);
}
