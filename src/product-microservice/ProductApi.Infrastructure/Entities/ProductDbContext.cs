using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ProductApi.Infrastructure.Entities;

public partial class ProductDbContext : DbContext
{
    public ProductDbContext()
    {
    }

    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Categorie> Categories { get; set; }

    public virtual DbSet<Product> Products { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categorie>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("categorie", tb => tb.HasComment("table des catégories"));

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("product", tb => tb.HasComment("table des produits"));

            entity.HasIndex(e => e.Idcategorie, "idx_product_categorie");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("binary(16)")
                .IsRequired();
            entity.Property(e => e.Actif).HasColumnName("actif");
            entity.Property(e => e.Datecreation)
                .HasColumnType("datetime")
                .HasColumnName("datecreation");
            entity.Property(e => e.Datemodification)
                .HasColumnType("datetime")
                .HasColumnName("datemodification");
            entity.Property(e => e.Description)
                .HasMaxLength(250)
                .HasColumnName("description");
            entity.Property(e => e.Idcategorie).HasColumnName("idcategorie");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .HasColumnName("name");
            entity.Property(e => e.Prix)
                .HasPrecision(10)
                .HasColumnName("prix");
            entity.Property(e => e.Qtestock).HasColumnName("qtestock");

            entity.HasOne(d => d.IdcategorieNavigation).WithMany(p => p.Products)
                .HasForeignKey(d => d.Idcategorie)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_categorie");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
