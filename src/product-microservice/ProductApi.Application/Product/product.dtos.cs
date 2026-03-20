namespace ProductApi.Application.Product;

public class AddProductRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Decimal? Amount { get; set; }
    public int? Quantity { get; set; }
    public DateTime? DateCreation { get; set; }
    public DateTime? DateModification { get; set; }
    public bool? Actif { get; set; }
    public int? IdCategorie { get; set; }
}
public class UpdateProductRequest : AddProductRequest
{
    public Guid? Id { get; set; }
}

public class CategorieRequest
{
    public string? Name { get; set; } = string.Empty;
}

public class ProductResponse
{
    public string? Id { get; set; }
    public string? Name { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public Decimal? Amount { get; set; }
    public int? Quantity { get; set; }
    public DateTime? DateCreation { get; set; }
    public DateTime? DateModification { get; set; }
    public bool? Actif { get; set; } = true;
    public int? IdCategorie { get; set; }
    public CategorieResponse? Categorie { get; set; }
}

public class CategorieResponse
{
    public int? Id { get; set; }
    public string? Name { get; set; } = string.Empty;
}

public static class ProductConst
{
    public const string ItemCacheKeyPrefix = "product";
    public const string GetAllCacheKeyPrefix = "get-allproduct";
}
