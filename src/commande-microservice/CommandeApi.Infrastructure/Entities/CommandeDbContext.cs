using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CommandeApi.Infrastructure.Entities;

public partial class CommandeDbContext : DbContext
{
    public CommandeDbContext()
    {
    }

    public CommandeDbContext(DbContextOptions<CommandeDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Commande> Commandes { get; set; }

    public virtual DbSet<ProductCommande> ProductCommandes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Commande>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.DateCommande).HasDefaultValueSql("'current_timestamp()'");
        });

        modelBuilder.Entity<ProductCommande>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasOne(d => d.Commande).WithMany(p => p.ProductCommandes).HasConstraintName("FK_Product_Commande");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
