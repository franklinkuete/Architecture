using CommandeApi.Domain.Models;

namespace CommandeApi.Application.Commande;

// Request
public record CommandeRequest(
    int? Id,
DateTime CommandeDate,
string Libelle,
string Description,
    string ClientId
)
{
    public List<ProductCommandeRequest> ProductItems { get; set; } = new();
}

public record ProductCommandeRequest(
   string ProductName,
   string ProductId,
   int Qte,
   decimal Amount
   );

public record AddProductItemsRequest 
{
  public  List<ProductCommandeRequest> ProductItems { get; set; } = new();
  public  int? CommandeId { get; set; } 

};


// Response
public record CommandeResponse(int Id,
   string Libelle,
   string Description,
   string ClientId,
    DateTime DateCommande,
    StatutCommande Statut,
List<ProductCommandeResponse> ProductItems
   );
public record ProductCommandeResponse(
    int Id,
    string ProductName,
    string ProductId,
    int Qte,
    decimal Amount,
    string CommandeId
    );

public static class CommandeConst
{
    public const string GetAllCacheKeyPrefix = "get-allcommande";
    public const string ItemCacheKeyPrefix = "commande";
}