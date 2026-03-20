using Mapster;
using MySqlX.XDevAPI;
using ProductApi.Application.Product;
using ProductApi.Domain.Models;
using ProductApi.Infrastructure.Entities;

namespace ProductApi.Infrastructure.Mappings
{
    public class ProductMappingConfiguration : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {

            config.NewConfig<ProductPOCO, ProductResponse>()
             .Map(dest => dest.Id, src => src.Id!.ToString())
             .Map(dest => dest.IdCategorie, src => src.Categorie!.Id.ToString())
             .Map(dest => dest.Categorie, src => new Categorie { Id = src.Categorie!.Id , Name=src.Categorie.CategorieName});

            config.NewConfig<ProductPOCO, Product>()
             .Map(dest => dest.Actif, src => src.Actif)
             .Map(dest => dest.Name, src => src.Name)
             .Map(dest => dest.Datecreation, src => src.DateCreation)
             .Map(dest => dest.Datemodification, src => src.DateModification)
             .Map(dest => dest.Description, src => src.Description)
             .Map(dest => dest.Id, src => src.Id)
             .Map(dest => dest.Prix, src => src.Amount)
             .Map(dest => dest.Qtestock, src => src.Quantity)
             .Map(dest => dest.Idcategorie, src => src.Categorie!.Id);

            config.NewConfig<Product, ProductPOCO>()
             .Map(dest => dest.Actif, src => src.Actif)
             .Map(dest => dest.Name, src => src.Name)
             .Map(dest => dest.DateCreation, src => src.Datecreation)
             .Map(dest => dest.DateModification, src => src.Datemodification)
             .Map(dest => dest.Description, src => src.Description)
             .Map(dest => dest.Id, src => src.Id)
             .Map(dest => dest.Amount, src => src.Prix)
             .Map(dest => dest.Quantity, src => src.Qtestock)
             .Map(dest => dest.Categorie!.Id, src => src.Idcategorie)
             .Map(dest => dest.Categorie!.CategorieName, src => src.IdcategorieNavigation.Name);

            config.NewConfig<AddProductRequest, ProductPOCO>()
             .Map(dest => dest.Actif, src => src.Actif)
             .Map(dest => dest.Name, src => src.Name)
             .Map(dest => dest.DateCreation, src => src.DateCreation)
             .Map(dest => dest.DateModification, src => src.DateModification)
             .Map(dest => dest.Description, src => src.Description)
             .Map(dest => dest.Amount, src => src.Amount)
             .Map(dest => dest.Quantity, src => src.Quantity)
             .Map(dest => dest.Categorie, src => new Categorie { Id = src.IdCategorie!.Value });

            config.NewConfig<UpdateProductRequest, ProductPOCO>()
            .Map(dest => dest.Actif, src => src.Actif)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.DateCreation, src => src.DateCreation)
            .Map(dest => dest.DateModification, src => src.DateModification)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.Amount, src => src.Amount)
            .Map(dest => dest.Quantity, src => src.Quantity)
            .Map(dest => dest.Categorie,
                 src => src.IdCategorie.HasValue
                     ? new CategoriePOCO { Id = src.IdCategorie.Value }
                     : null)
            .IgnoreNullValues(true);



            config.NewConfig<CategoriePOCO, Categorie>()
             .Map(dest => dest.Id, src => src.Id)
             .Map(dest => dest.Name, src => src.CategorieName);

            config.NewConfig<Categorie, CategoriePOCO>()
             .Map(dest => dest.Id, src => src.Id)
             .Map(dest => dest.CategorieName, src => src.Name);

            config.NewConfig<CategoriePOCO, CategorieResponse>()
             .Map(dest => dest.Id, src => src.Id)
             .Map(dest => dest.Name, src => src.CategorieName);
        }
    }
}
