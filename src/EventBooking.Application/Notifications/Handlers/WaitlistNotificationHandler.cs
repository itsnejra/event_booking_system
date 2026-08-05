using EventBooking.Application.Abstractions;
using EventBooking.Domain.Abstractions;
using EventBooking.Domain.Events;

namespace EventBooking.Application.Notifications.Handlers;

/// <summary>
/// Seats came back, so the front of the queue gets told. Being notified deliberately does not book
/// anything - it is an invitation to buy, not a purchase made on somebody's behalf.
/// </summary>
public sealed class WaitlistNotificationHandler(
    IEventRepository events,
    CustomerNotifier notifier,
    IClock clock) : IDomainEventHandler<TicketsReleasedDomainEvent>
{
    public void Handle(TicketsReleasedDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var @event = events.FindById(domainEvent.EventId);
        if (@event is null || @event.Status != EventStatus.Published)
        {
            return;
        }

        foreach (var entry in @event.TakeWaitlistCandidates(domainEvent.Quantity, clock.UtcNow))
        {
            notifier.Notify(
                entry.CustomerId,
                $"Tickets available for {@event.Title}",
                $"{domainEvent.Quantity} ticket(s) for {@event.Title} have been released. "
                + $"You asked for {entry.RequestedQuantity} - book now, they go on a first come, first served basis.");
        }
    }
}
