using Ardalis.Result;
using Core.Events;
using Core.Interfaces;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductApi.Domain.Models;
using ProductApi.Infrastructure.Entities;

namespace ProductApi.Infrastructure.Repositories;

internal class ProductRepository : IProductRepository
{
    private readonly IRepositoryBase<Product> _persistenceProduct;
    private readonly IRepositoryBase<Categorie> _persistenceCategorie;
    private readonly ILogger<ProductRepository> _logger;
    public ProductRepository(IRepositoryBase<Product> persistenceProduct, IRepositoryBase<Categorie> persistenceCategorie, ILogger<ProductRepository> logger)
    {
        _persistenceProduct = persistenceProduct;
        _persistenceCategorie = persistenceCategorie;
        _logger = logger;
    }

    public async Task<ProductPOCO> AddProductAsync(ProductPOCO Product)
    {
        var entity = Product.Adapt<Product>();

        var categorieResult = await _persistenceCategorie.GetByIdAsync(Product.Categorie!.Id);

        var resultat = await _persistenceProduct.AddAsync(entity);
        return resultat.Adapt<ProductPOCO>();
    }

    public async Task<bool> CategorieExistAsync(int idCategorie)
    {
        return await _persistenceProduct.GetAll()
            .AnyAsync(p => p.Idcategorie == idCategorie);
    }

    public async Task<IEnumerable<ProductPOCO>> GetAllProductAsync()
    {
        var resultat = _persistenceProduct.GetAll();
        return resultat.ToList().Adapt<IEnumerable<ProductPOCO>>();
    }
    public async Task<IEnumerable<ProductPOCO>> GetAllProductAsync(int pageIndex = 0, int pageSize = 50)
    {
        // 1. On construit la requête (sans await ici)
        var query = _persistenceProduct.GetAll()
            .Include(p => p.IdcategorieNavigation)
            .Select(p => new ProductPOCO
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Amount = p.Prix,
                Quantity = p.Qtestock,
                DateCreation = p.Datecreation,
                DateModification = p.Datemodification,
                Actif = p.Actif == 1,
                Categorie = new CategoriePOCO
                {
                    Id = p.Idcategorie,
                    CategorieName = p.IdcategorieNavigation.Name
                }
            })
            .OrderBy(p => p.Id)
            .Skip(pageIndex * pageSize)
            .Take(pageSize);

        // 2. C'est ici que l'appel SQL est réellement envoyé à la base
        return await query.ToListAsync();
    }

    public async Task<ProductPOCO?> GetProductByIdAsync(Guid id)
    {
        var resultat = await _persistenceProduct.GetByIdAsync(id);
        return resultat.Adapt<ProductPOCO>();
    }

    public async Task<bool> RemoveProductAsync(Guid Id)
    {
        return await _persistenceProduct.DeleteAsync(Id);
    }

    public async Task<ProductPOCO?> UpdateProductAsync(ProductPOCO product)
    {
        var existingProduct = await _persistenceProduct.GetByIdAsync(product.Id!);

        if (existingProduct != null)
        {
            if (product.Actif.HasValue)
            {
                existingProduct.Actif = product.Actif.Value ? (sbyte)1 : (sbyte)0;
            }

            if (!string.IsNullOrEmpty(product.Description))
            {
                existingProduct.Description = product.Description;
            }

            if (!string.IsNullOrEmpty(product.Name))
            {
                existingProduct.Name = product.Name;
            }

            if (product.Amount.HasValue)
            {
                existingProduct.Prix = product.Amount.Value;
            }

            if (product.Quantity.HasValue)
            {
                existingProduct.Qtestock = product.Quantity.Value;
            }

            if (product.Categorie != null && product.Categorie.Id > 0)
            {
                existingProduct.Idcategorie = product.Categorie.Id;
            }

            // update
            existingProduct.Datemodification = DateTime.Now;
            var result = await _persistenceProduct.UpdateAsync(product.Id!, existingProduct);
            return result.Adapt<ProductPOCO>();
        }

        return null;

    }

    private async Task<Result<Dictionary<Guid, Product>>> GetAndValidateStockAsync(List<ProductStock> productItems)
    {
        if (productItems == null || !productItems.Any())
            return Result.Invalid(new ValidationError { ErrorMessage = "La liste est vide." });

        var productIds = productItems.Select(p => Guid.Parse(p.ProductId)).Distinct().ToList();

        // Convertissez explicitement en tableau pour aider le traducteur EF
        var productIdsArray = productItems
            .Select(p => Guid.Parse(p.ProductId))
            .Distinct()
            .ToArray(); // Utiliser ToArray() au lieu de ToList()

        // Utilisez le tableau dans la requête
        var productsList = await _persistenceProduct.GetAll()
            .Where(p => productIdsArray.Contains(p.Id))
            .ToListAsync();

        var productsInDb = productsList.ToDictionary(p => p.Id);

        // 1. Vérification d'existence globale
        if (productsInDb.Count != productIds.Count)
            return Result.NotFound("Un ou plusieurs produits sont introuvables.");

        // 2. Vérification du stock
        var validationErrors = new List<ValidationError>();
        foreach (var item in productItems)
        {
            var product = productsInDb[Guid.Parse(item.ProductId)];
            if (product.Qtestock < item.Quantity)
            {
                validationErrors.Add(new ValidationError
                {
                    Identifier = item.ProductId,
                    ErrorMessage = $"Stock insuffisant pour le produit {item.ProductId} - quantité demandée {item.Quantity}. (Dispo: {product.Qtestock})"
                });
            }
        }

        return validationErrors.Any()
            ? Result.Invalid(validationErrors)
            : Result.Success(productsInDb); // On retourne le dictionnaire pour l'étape suivante
    }

    public async Task<List<string>> UpdateStock(CommandeCreatedEvent request)
    {
        // On récupère le résultat qui contient DÉJÀ nos entités chargées
        var checkResult = await GetAndValidateStockAsync(request.products);

        if (!checkResult.IsSuccess)
        {
            return checkResult.Errors
                .Concat(checkResult.ValidationErrors.Select(e => e.ErrorMessage))
                .ToList();
        }

        // On récupère le dictionnaire depuis la "Value" du Result
        var productsInDb = checkResult.Value;

        foreach (var item in request.products)
        {
            var productId = Guid.Parse(item.ProductId);
            var product = productsInDb[productId];

            product.Qtestock -= item.Quantity;
            product.Datemodification = DateTime.Now;

            // Mise à jour via votre repository
            await _persistenceProduct.UpdateAsync(productId, product);
        }

        return new List<string>();
    }



    public async Task<List<ProductPOCO?>> UpdateStock(CommandeCancelEvent request)
    {
        var liste = new List<ProductPOCO?>();

        foreach (var item in request.products)
        {
            // 1. Conversion du string en Guid
            if (Guid.TryParse(item.ProductId, out Guid productGuid))
            {
                // 2. On passe le Guid (et non le string)
                var existingProduct = await _persistenceProduct.GetByIdAsync(productGuid);

                if (existingProduct != null)
                {
                    // Logique métier : on décrémente le stock

                    existingProduct.Qtestock += item.Quantity;

                    existingProduct.Datemodification = DateTime.Now;

                    // 3. Assure-toi que UpdateAsync accepte aussi le Guid
                    var result = await _persistenceProduct.UpdateAsync(productGuid, existingProduct);
                    liste.Add(result.Adapt<ProductPOCO>());
                }
            }
            else
            {
                // Optionnel : Loguer que l'ID reçu de Kafka n'est pas un GUID valide
                Console.WriteLine($"ID invalide reçu : {item.ProductId}");
            }
        }

        return liste;
    }

}