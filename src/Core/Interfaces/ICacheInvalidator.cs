namespace Core.Interfaces;

// Interface pour les commandes (Handler) qui nécessitent une invalidation du cache après leur exécution réussie.
public interface ICacheInvalidator
{
    // Les clés de cache à supprimer (ex: "products-all", "user-123")
    List<string> CacheKeysToInvalidate { get; }
}
