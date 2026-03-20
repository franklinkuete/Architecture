using CommandeApi.Domain.Interfaces;
using CommandeApi.Domain.Models;
using Core.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommandeApi.Infrastructure.Repositories;

public class CommandeRepository : ICommandeRepository
{
    private readonly IRepositoryBase<CommandeApi.Infrastructure.Entities.Commande> _persistenceCommande;
    private readonly IRepositoryBase<CommandeApi.Infrastructure.Entities.ProductCommande> _persistenceProductCommande;
    private readonly ILogger<CommandeRepository> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CommandeRepository(IRepositoryBase<CommandeApi.Infrastructure.Entities.Commande> persistenceCommande,
        IRepositoryBase<CommandeApi.Infrastructure.Entities.ProductCommande> persistenceProductCommande,
        ILogger<CommandeRepository> logger, IHttpContextAccessor httpContextAccessor)
    {
        _persistenceCommande = persistenceCommande;
        _logger = logger;
        _persistenceProductCommande = persistenceProductCommande;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _persistenceCommande.DeleteAsync(id);
    }

    public async Task<Commande?> GetCommandeByIdAsync(int id)
    {
        var commandeDb = await _persistenceCommande.GetAll()
        .Include(c => c.ProductCommandes) // Remplacez par le nom exact de votre propriété de navigation
        .FirstOrDefaultAsync(c => c.Id == id);

        _logger.LogInformation("Récupération de la commande avec ID {Id} : {CommandeDb}", id, commandeDb);
        return commandeDb.Adapt<Commande>();
    }

    public async Task<IEnumerable<Commande>> GetAllCommandeAsync(int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _persistenceCommande.GetAll()
                                       .Include(c => c.ProductCommandes)
                                       .OrderBy(p => p.Id)
                                       .Skip(pageIndex * pageSize)
                                       .Take(pageSize);

        var resultat = await query.ToListAsync();

        _logger.LogInformation("Récupération de toutes les commandes : {Resultat trouvé}", resultat.Count);

        return resultat.Adapt<IEnumerable<Commande>>();
    }

    public async Task<Commande> AddCommandeAsync(Commande commande)
    {
        // Utilisez explicitement la config globale (ou votre config spécifique)
        var entityCommande = commande.Adapt<CommandeApi.Infrastructure.Entities.Commande>();
        entityCommande.Statut = (int)StatutCommande.Completed;

        var resultat = await _persistenceCommande.AddAsync(entityCommande);

        // Retourne l'objet mappé en sens inverse
        return resultat.Adapt<Commande>(TypeAdapterConfig.GlobalSettings);
    }


    public async Task<Commande?> UpdateCommandeAsync(int id, Commande commande)
    {
        var entity = commande.Adapt<CommandeApi.Infrastructure.Entities.Commande>();
        var resultat = await _persistenceCommande.UpdateAsync(id, entity);
        return resultat.Adapt<Commande>();
    }

    // TODO : à optimiser
    public async Task<Commande?> RestoreStockAfterCompensation(int id, List<ProductItem> productToRetrieve)
    {
        // 1. Charger la commande avec ses lignes
        var commandeEntity = await _persistenceCommande.GetAll()
            .Include(c => c.ProductCommandes)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (commandeEntity == null)
            return null;

        // 2. Identifier les produits à conserver
        var idsToKeep = productToRetrieve.Select(r => r.ProductId).ToHashSet();

        // 3. Supprimer les lignes non désirées
        var itemsToDelete = commandeEntity.ProductCommandes
            .Where(p => !idsToKeep.Contains(p.ProduitId))
            .ToList();

        foreach (var item in itemsToDelete)
        {
            // si la le produit existe en base de données, on le supprime
            commandeEntity.ProductCommandes.Remove(item);
        }

        // 4. Mettre à jour le statut
        commandeEntity.Statut = (int)StatutCommande.CheckingStock;

        // 5. Sauvegarder via le repo
        var updated = await _persistenceCommande.UpdateAsync(commandeEntity.Id, commandeEntity);

        // 6. Retourner le DTO
        return updated.Adapt<Commande>();
    }

    public async Task<bool> DeleteCommandeAsync(int id)
    {
        return await _persistenceCommande.DeleteAsync(id);
    }

    public async Task<IEnumerable<Commande>> GetAllCommandeByClientIdAsync(
    string clientId,
    int pageIndex = 0,
    int pageSize = int.MaxValue)
    {
        var query = _persistenceCommande.GetAll()
                                       .Include(c => c.ProductCommandes)
                                       .OrderByDescending(c => c.Id)
                                       .Where(c => c.ClientId == clientId)
                                       .Skip(pageIndex * pageSize)
                                       .Take(pageSize);

        // Adaptation et exécution asynchrone en base
        var commandes = await query.ProjectToType<Commande>().ToListAsync();

        return commandes;
    }

    public async Task<List<Commande>> GetAllCommandes(int pageIndex = 0, int pageSize = int.MaxValue)
    {
        // On récupère toutes les commandes
        var query = _persistenceCommande.GetAll()
                                       .Include(c => c.ProductCommandes)
                                       .OrderByDescending(c => c.Id)
                                       .Skip(pageIndex * pageSize)   // saute les éléments des pages précédentes
                                       .Take(pageSize);              // limite au nombre d’éléments demandé

        // Adaptation vers la liste de Commande
        var result = query.Adapt<List<Commande>>();

        // Comme Adapt est synchrone, on enveloppe le résultat dans un Task
        return await Task.FromResult(result);

    }

    public async Task<Commande> DeleteItems(List<int> ids)
    {
        if (ids == null || !ids.Any())
            throw new ArgumentException("La liste d'identifiants ne peut pas être vide.", nameof(ids));

        // 1. Récupérer le Queryable depuis le repo
        var queryable = _persistenceCommande.GetAll();

        // 2. Trouver l'entité avec ses relations (Chargement SQL optimisé)
        var commandeEntity = await queryable
            .Include(c => c.ProductCommandes)
            .FirstOrDefaultAsync(c => c.ProductCommandes.Any(p => ids.Contains(p.Id)));

        if (commandeEntity == null)
            throw new KeyNotFoundException($"Aucune commande trouvée contenant les IDs : {string.Join(", ", ids)}");

        // 3. Identifier les items à supprimer
        var itemsASupprimer = commandeEntity.ProductCommandes
            .Where(p => ids.Contains(p.Id))
            .ToList();

        // 4. Vérifier la cohérence des IDs
        if (itemsASupprimer.Count != ids.Count)
        {
            var idsTrouves = itemsASupprimer.Select(p => p.Id);
            var idsManquants = ids.Except(idsTrouves);
            throw new ArgumentException($"Certains produits n'appartiennent pas à la commande {commandeEntity.Id} : {string.Join(", ", idsManquants)}");
        }

        // 5. Suppression par retrait de la collection
        foreach (var item in itemsASupprimer)
        {
            commandeEntity.ProductCommandes.Remove(item);
        }

        // 6. Sauvegarde en base de données
        await _persistenceCommande.UpdateAsync(commandeEntity.Id, commandeEntity);

        // 7. MAPPING : On transforme l'Entity (Infrastructure) en Model (Domain/DTO)
        // Assure-toi d'avoir : using Mapster;
        return commandeEntity.Adapt<Commande>();
    }


    public async Task<Commande> AddItems(List<ProductCommande> items)
    {
        var commandeId = items.First().CommandeId;

        // 1. Récupérer la commande avec tracking
        var commandeEntity = await _persistenceCommande.DataContext
            .Include(x => x.ProductCommandes)
            .FirstOrDefaultAsync(c => c.Id == commandeId);

        if (commandeEntity != null)
            commandeEntity!.Statut = (int)StatutCommande.Completed;

        foreach (var itemDto in items)
        {
            var existingItem = commandeEntity!.ProductCommandes
                .FirstOrDefault(p => p.ProduitId == itemDto.ProduitId);

            if (existingItem != null)
            {
                // si le produit existe déjà, on augmente juste sa quantité. Pour ne pas ajouté plusieurs ligne du même produit Identique dans la même commande
                existingItem.Quantite += itemDto.Quantite;
            }
            else
            {
                // Correction : Mapper vers l'entité d'infrastructure ET l'ajouter à la collection
                var newEntity = itemDto.Adapt<CommandeApi.Infrastructure.Entities.ProductCommande>();

                // On s'assure que la clé étrangère est bien liée à l'entité trackée
                newEntity.CommandeId = commandeEntity.Id;

                commandeEntity.ProductCommandes.Add(newEntity);
            }
        }

        // 3. Retourner le résultat mappé (le reload est inutile si SaveAsync a réussi et que le tracking est actif)
        return commandeEntity!.Adapt<Commande>();
    }
}
