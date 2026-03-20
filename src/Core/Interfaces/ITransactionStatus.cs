namespace Core.Interfaces;

public interface ITransactionStatus
{
    // Utilisation d'une liste d'actions asynchrones
    List<Func<CancellationToken, Task>> PostCommitActions { get; }
}