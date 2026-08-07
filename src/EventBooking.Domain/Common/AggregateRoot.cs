namespace EventBooking.Domain.Common;

/// <summary>
/// An entity that owns a consistency boundary. Everything inside an aggregate is only ever changed
/// through its root, which is what keeps the invariants in one place instead of scattered across
/// services. Aggregates record what happened; publishing those records is somebody else's job.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : struct
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(Guard.NotNull(domainEvent));
}
