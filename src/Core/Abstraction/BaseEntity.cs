

using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Abstraction;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    // Liste interne pour capturer les événements métier
    private readonly List<object> _domainEvents = new();

    // Exposition en lecture seule
    [NotMapped] // Ne pas créer de colonne en base pour ça
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(object domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

