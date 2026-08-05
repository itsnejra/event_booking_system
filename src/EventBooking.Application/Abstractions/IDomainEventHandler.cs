using EventBooking.Domain.Common;

namespace EventBooking.Application.Abstractions;

/// <summary>
/// Reacts to something that already happened. Handlers are how side effects - e-mails, waiting list
/// promotions - get attached to the domain without the domain knowing they exist.
/// </summary>
/// <remarks>
/// Contravariant so that a handler written against a base event type can be registered for a more
/// specific one.
/// </remarks>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    void Handle(TDomainEvent domainEvent);
}
