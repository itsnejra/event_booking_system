using EventBooking.Domain.Common;

namespace EventBooking.Application.Abstractions;

/// <summary>
/// Drains the events an aggregate collected and hands them to whoever is listening.
/// </summary>
/// <remarks>
/// Application services call this once, at the end of an operation, when the aggregates involved are
/// in a consistent state. Dispatching from inside the domain would mean side effects firing halfway
/// through a change that might still be rolled back.
/// </remarks>
public interface IDomainEventDispatcher
{
    void Dispatch(params IHasDomainEvents[] aggregates);
}
