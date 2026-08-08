
namespace EventBooking.Domain.Common;

/// <summary>
/// Non-generic view over an aggregate that has collected domain events, so that the dispatcher can
/// work with a heterogeneous set of aggregates without knowing their identifier types.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
