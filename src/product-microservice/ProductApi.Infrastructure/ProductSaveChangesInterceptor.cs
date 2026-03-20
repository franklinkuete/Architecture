using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using ProductApi.Infrastructure.Entities;

namespace ProductApi.Infrastructure;

public class ProductSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<ProductSaveChangesInterceptor> _logger;
    public ProductSaveChangesInterceptor(ILogger<ProductSaveChangesInterceptor> logger)
    {
        _logger = logger;
    }
    // MÉTHODE POUR LE SYNCHRONE (ex: context.SaveChanges())
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateDates(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    // MÉTHODE POUR L'ASYNCHRONE (ex: await context.SaveChangesAsync())
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateDates(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateDates(DbContext? context)
    {
        if (context == null) return;

        // On ne filtre QUE sur les entités qui nous intéressent (Added ou Modified)
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Product product)
            {
                var now = DateTime.UtcNow;

                if (entry.State == EntityState.Added)
                {
                    // On ne génère l'ID que s'il est vide pour éviter de l'écraser
                    if (product.Id == Guid.Empty)
                        product.Id = Guid.NewGuid();

                    product.Datecreation = now;
                    product.Datemodification = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    product.Datemodification = now;
                }
            }
        }
    }
}
