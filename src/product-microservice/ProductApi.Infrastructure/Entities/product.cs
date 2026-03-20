namespace ProductApi.Infrastructure.Entities;

/// <summary>
/// table des produits
/// </summary>
public partial class Product
{
    public Guid Id { get; set; } 

    public string? Name { get; set; }

    public string? Description { get; set; }

    public decimal? Prix { get; set; }

    public int Qtestock { get; set; }

    public DateTime Datecreation { get; set; }

    public DateTime Datemodification { get; set; }

    public sbyte? Actif { get; set; }

    public int Idcategorie { get; set; }

    public virtual Categorie IdcategorieNavigation { get; set; } = null!;
}
