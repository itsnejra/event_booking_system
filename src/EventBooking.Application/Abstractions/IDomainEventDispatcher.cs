using EventBooking.Domain.Common;

namespace EventBooking.Application.Abstractions;

/// <summary>
/// Drains the events an aggregate collected and hands them to whoever is listening.
/// </summary>
public interface IDomainEventDispatcher
{
    void Dispatch(params IHasDomainEvents[] aggregates);
}
