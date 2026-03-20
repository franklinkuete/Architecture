using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CommandeApi.Infrastructure.Entities;

[Table("ProductCommande")]
[Index("CommandeId", Name = "idx_commande_id")]
public partial class ProductCommande
{
    [Key]
    [Column(TypeName = "int(11)")]
    public int Id { get; set; }

    [StringLength(255)]
    public string NomProduit { get; set; } = null!;

    [StringLength(100)]
    public string ProduitId { get; set; } = null!;

    [Column(TypeName = "int(11)")]
    public int Quantite { get; set; }

    public decimal PrixUnitaire { get; set; }

    [Column(TypeName = "int(11)")]
    public int CommandeId { get; set; }

    [ForeignKey("CommandeId")]
    [InverseProperty("ProductCommandes")]
    public virtual Commande Commande { get; set; } = null!;
}
