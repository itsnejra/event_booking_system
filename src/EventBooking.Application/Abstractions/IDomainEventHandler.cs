using EventBooking.Domain.Common;

namespace EventBooking.Application.Abstractions;

/// <summary>
/// Reacts to something that already happened. Handlers are how side effects - e-mails, waiting list
/// promotions - get attached to the domain without the domain knowing they exist.
/// </summary>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    void Handle(TDomainEvent domainEvent);
}
