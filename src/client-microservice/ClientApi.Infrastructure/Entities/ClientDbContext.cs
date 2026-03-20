using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ClientApi.Infrastructure.Entities;

public partial class ClientDbContext : DbContext
{
    public ClientDbContext()
    {
    }

    public ClientDbContext(DbContextOptions<ClientDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Client> Clients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("client", "microservice");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Codepostal)
                .HasMaxLength(20)
                .HasColumnName("codepostal");
            entity.Property(e => e.Datecreation).HasColumnName("datecreation");
            entity.Property(e => e.Datemodification).HasColumnName("datemodification");
            entity.Property(e => e.Datenaissance).HasColumnName("datenaissance");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Firstname)
                .HasMaxLength(100)
                .HasColumnName("firstname");
            entity.Property(e => e.Lastname)
                .HasMaxLength(100)
                .HasColumnName("lastname");
            entity.Property(e => e.Telephone)
                .HasMaxLength(50)
                .HasColumnName("telephone");
            entity.Property(e => e.Ville)
                .HasMaxLength(100)
                .HasColumnName("ville");
        });

      

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

}
