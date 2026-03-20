using CommandeApi.Application.Commande;
using CommandeApi.Domain.Models;
using Core.Events;
using Mapster;

namespace CommandeApi.Infrastructure.Mappings
{
    public class CommandeMappingConfiguration : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {

            config.NewConfig<AddProductItemsRequest, Commande>()
                .Map(dest => dest.ProductItems, src => src.ProductItems.Adapt<List<ProductCommande>>())
                .AfterMapping((src, dest) =>
                {
                    foreach (var item in dest.ProductItems)
                    {
                        item.CommandeId = src.CommandeId ?? 0;
                    }
                });

            config.NewConfig<ProductStock, ProductItem>();

            config.NewConfig<ProductItem, ProductStock>();


            config.NewConfig<CommandeApi.Infrastructure.Entities.Commande, Commande>()
            .Map(dest => dest.Date, src => src.DateCommande) 
            .Map(dest => dest.ProductItems, src => src.ProductCommandes);

            config.NewConfig<Commande, CommandeApi.Infrastructure.Entities.Commande>()
                 .Map(dest => dest.DateCommande, src => src.Date)
                 .Map(dest => dest.ProductCommandes, src => src.ProductItems);

            config.NewConfig<CommandeApi.Infrastructure.Entities.ProductCommande, ProductCommande>()
                .Map(dest => dest.Quantite, src => src.Quantite)
                .Map(dest => dest.PrixUnitaire, src => src.PrixUnitaire)
                .Map(dest => dest.CommandeId, src => src.CommandeId)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.ProductName, src => src.NomProduit)
                .Map(dest => dest.ProduitId, src => src.ProduitId);

            config.NewConfig<Commande, CommandeApi.Infrastructure.Entities.Commande>()
                 //.Map(dest => dest.ClientId, src => src.ClientId)
                 .Map(dest => dest.DateCommande, src => src.Date)
                 //.Map(dest => dest.Description, src => src.Description)
                 //.Map(dest => dest.Id, src => src.Id)
                 //.Map(dest => dest.Libelle, src => src.Libelle)
                 //.Map(dest => dest.Statut, src => src.Statut)
                 .Map(dest => dest.ProductCommandes, src => src.ProductItems);

            config.NewConfig<ProductCommande, CommandeApi.Infrastructure.Entities.ProductCommande>()
                .Map(dest => dest.Quantite, src => src.Quantite)
                .Map(dest => dest.PrixUnitaire, src => src.PrixUnitaire)
                .Map(dest => dest.CommandeId, src => src.CommandeId)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.NomProduit, src => src.ProductName)
                .Map(dest => dest.ProduitId, src => src.ProduitId);

            config.NewConfig<CommandeRequest, Commande>()
                .Map(dest => dest.Date, src => DateTime.UtcNow)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.Libelle, src => src.Libelle)
                .Map(dest => dest.ClientId, src => src.ClientId)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.ProductItems, src => src.ProductItems);
                
            config.NewConfig<ProductCommandeRequest, ProductCommande>()
                .Map(dest => dest.ProductName, src => src.ProductName)
                .Map(dest => dest.Quantite, src => src.Qte)
                .Map(dest => dest.PrixUnitaire, src => src.Amount)
                .Map(dest => dest.ProduitId, src => src.ProductId);


            config.NewConfig<Commande, CommandeResponse>()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.DateCommande, src => src.Date)
                .Map(dest => dest.Libelle, src => src.Libelle)
                .Map(dest => dest.ClientId, src => src.ClientId)
                .Map(dest => dest.Statut, src => src.Statut)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.ProductItems, src => src.ProductItems.Adapt<List<ProductCommandeResponse>>());

            config.NewConfig<ProductCommande, ProductCommandeResponse>()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.ProductId, src => src.ProduitId)
                .Map(dest => dest.ProductName, src => src.ProductName)
                .Map(dest => dest.Qte, src => src.Quantite)
                .Map(dest => dest.Amount, src => src.PrixUnitaire)
                .Map(dest => dest.CommandeId, src => src.CommandeId);
        }
    }
}
