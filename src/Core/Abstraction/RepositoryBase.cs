using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace Core.Abstraction;

/// <summary>
/// Définition d'un Repository Générique de base.
/// </summary>
/// <typeparam name="T">
/// 'T' représente l'entité de domaine (ex: Commande, Produit).
/// Le mot-clé 'where T : class' est une contrainte générique indispensable : 
/// elle garantit que 'T' est un type référence (une classe), ce qui permet 
/// à Entity Framework Core de l'utiliser dans un DbSet<T>.
/// </typeparam>
public class RepositoryBase<T> : IRepositoryBase<T> where T : class
{
    protected readonly DbContext _context;
    // Utilisation d'une propriété pour accéder au DbSet correspondant à l'entité T
    protected DbSet<T> db => _context.Set<T>();

    public RepositoryBase(DbContext context) => _context = context;

    /// <summary>
    /// Retourne la requête brute sans exécution immédiate. 
    /// L'asynchronisme (ex: .ToListAsync()) se fera au dernier moment dans le Handler.
    /// .AsNoTracking() désactive le suivi des modifications : gain de performance majeur pour la lecture.
    /// </summary>
    public IQueryable<T> GetAll() => db.AsNoTracking();

    /// <summary>
    /// Recherche une entité par sa clé primaire. 
    /// FindAsync est efficace car il vérifie d'abord si l'entité est déjà en mémoire (cache EF) 
    /// avant de requêter la base de données.
    /// </summary>
    public async Task<T?> GetByIdAsync(object id)
        => await db.FindAsync(id);

    /// <summary>
    /// Ajoute une entité. 
    /// On utilise .Add() synchrone car l'opération de mise en mémoire est quasi instantanée. 
    /// Le travail asynchrone réel (IO réseau) se produit lors du SaveChangesAsync.
    /// </summary>
    public async Task<T> AddAsync(T entity)
    {
        db.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Suppression sécurisée : On récupère l'entité pour confirmer son existence 
    /// avant de marquer sa suppression dans le ChangeTracker.
    /// </summary>
    public async Task<bool> DeleteAsync(object id)
    {
        var entity = await db.FindAsync(id);
        if (entity == null) return false;

        db.Remove(entity); // Marque l'entité pour suppression
        await _context.SaveChangesAsync(); // Exécute le DELETE SQL
        return true;
    }

    /// <summary>
    /// Mise à jour partielle optimisée.
    /// On récupère l'entité existante et on injecte les nouvelles valeurs du DTO (entity).
    /// .SetValues() ne met à jour que les propriétés qui ont réellement changé dans le SQL.
    /// </summary>
    public async Task<T?> UpdateAsync(object id, T entity)
    {
        // 1. Localiser l'entité originale
        var existingEntity = await db.FindAsync(id);
        if (existingEntity is null) return null;

        // 2. Copier les valeurs de l'objet 'entity' vers 'existingEntity'
        _context.Entry(existingEntity).CurrentValues.SetValues(entity);

        // 3. Persister uniquement les changements détectés
        await _context.SaveChangesAsync();
        return existingEntity;
    }

    public DbSet<T> DataContext => _context.Set<T>();

}


