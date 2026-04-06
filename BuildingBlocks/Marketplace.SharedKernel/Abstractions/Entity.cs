namespace Marketplace.SharedKernel.Abstractions;

/// <summary>
/// Represents the base type for entities that participate in domain events.
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets the domain events raised by the entity.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the entity event collection.
    /// </summary>
    /// <param name="domainEvent">The event raised by the entity.</param>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Clears all pending domain events from the entity.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}