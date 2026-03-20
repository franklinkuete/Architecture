using System;
using System.Collections.Generic;

namespace ProductApi.Infrastructure.Entities;

/// <summary>
/// table des catégories
/// </summary>
public partial class Categorie
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
