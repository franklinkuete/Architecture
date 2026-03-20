using Ardalis.Result;
using CommandeApi.Domain.Models;

namespace CommandeApi.Domain.Interfaces;

public interface ICommandeRepository
{
    // Méthodes génériques
    Task<Commande?> GetCommandeByIdAsync(int id);
    Task<IEnumerable<Commande>> GetAllCommandeAsync(int pageIndex = 0, int pageSize = int.MaxValue);
    Task<Commande> AddCommandeAsync(Commande commande);
    Task<Commande?> UpdateCommandeAsync(int id, Commande commande);
    Task<bool> DeleteCommandeAsync(int id);

    // Méthodes spécifiques
    Task<IEnumerable<Commande>> GetAllCommandeByClientIdAsync(string clientId, int pageIndex = 0, int pageSize = int.MaxValue);
    Task<List<Commande>> GetAllCommandes(int pageIndex = 0, int pageSize = int.MaxValue);

    Task<Commande> AddItems(List<ProductCommande> items);
    Task<Commande> DeleteItems(List<int> Ids);
    Task<Commande?> RestoreStockAfterCompensation(int id, List<ProductItem> productToRetrieve);

}
