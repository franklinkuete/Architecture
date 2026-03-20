
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CommandeApi.Infrastructure.Entities;

[Table("Commande")]
[Index("ClientId", Name = "idx_client_id")]
public partial class Commande
{
    [Key]
    [Column(TypeName = "int(11)")]
    public int Id { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateCommande { get; set; }

    [Column("libelle")]
    [StringLength(255)]
    public string Libelle { get; set; } = null!;

    [StringLength(255)]
    public string Description { get; set; } = null!;

    [Column(TypeName = "int(11)")]
    public int Statut { get; set; }

    [StringLength(100)]
    public string ClientId { get; set; } = null!;

    [InverseProperty("Commande")]
    public virtual ICollection<ProductCommande> ProductCommandes { get; set; } = new List<ProductCommande>();
}
