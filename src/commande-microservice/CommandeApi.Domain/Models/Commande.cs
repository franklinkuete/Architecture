using System.ComponentModel;

namespace CommandeApi.Domain.Models;

public class Commande
{

    public int? Id { get; set; }

    public DateTime? Date { get; set; } = null;
    public string? Libelle { get; set; } 
    public string? Description { get; set; }
    public StatutCommande? Statut { get; set; } 

    // Référence stable vers le client
    public string? ClientId { get; set; }

    // Liste des produits commandés
    public List<ProductCommande> ProductItems { get; set; } = new();
}

public class ProductCommande
{
    public int Id { get; set; } 

    public string ProduitId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;
    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }

    // Relation avec Commande
    public int CommandeId { get; set; } 
}

public enum StatutCommande
{
    [Description("La commande a été créée sans produit.")]
    Initial = 0,

    [Description("Vérification de la disponibilité des articles en stock en cours.")]
    CheckingStock = 1,

    [Description("Le stock a été confirmé et la commande est officiellement validée.")]
    Completed = 2,

    [Description("La commande a été annulée (stock indisponible ou action utilisateur).")]
    Cancel = 3,
}

public record ProductItem(string ProductId, int Quantity);
