using System;
using System.Collections.Generic;
using System.Text;

namespace ProductApi.Domain.Models;

public class ProductPOCO
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Decimal? Amount { get; set; }
    public int? Quantity { get; set; }
    public DateTime? DateCreation { get; set; }
    public DateTime? DateModification { get; set; }
    public bool? Actif { get; set; }
    public CategoriePOCO? Categorie { get; set; } = new CategoriePOCO();
}
