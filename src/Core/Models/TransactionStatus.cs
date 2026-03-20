using Core.Interfaces;

namespace Core.Models;

public class TransactionStatus : ITransactionStatus
{
    // Liste d’actions à exécuter après le commit d’une transaction.
    // Chaque élément est une fonction asynchrone prenant un CancellationToken et retournant un Task.
    // Cela permet d’enregistrer dynamiquement des callbacks (ex. invalidation de cache, envoi d’événements, logs)
    // qui ne doivent être déclenchés qu’une fois la transaction validée avec succès.
    public List<Func<CancellationToken, Task>> PostCommitActions { get; } = new();
}