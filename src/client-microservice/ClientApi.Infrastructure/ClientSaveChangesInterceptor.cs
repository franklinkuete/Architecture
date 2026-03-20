using ClientApi.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClientApi.Infrastructure;

public class ClientSaveChangesInterceptor : SaveChangesInterceptor
{
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
            if (entry.Entity is Client client)
            {
                var now = DateOnly.FromDateTime(DateTime.UtcNow);

                if (entry.State == EntityState.Added)
                {
                    client.Datecreation = now;
                    client.Datemodification = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    client.Datemodification = now;
                }
            }
        }
    }
}
