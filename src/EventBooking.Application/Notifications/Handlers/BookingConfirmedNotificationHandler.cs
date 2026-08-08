using EventBooking.Application.Abstractions;
using EventBooking.Domain.DomainEvents;
using EventBooking.Domain.Entities;
using EventBooking.Domain.Interfaces;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Notifications.Handlers;

public sealed class BookingConfirmedNotificationHandler(IEventRepository events, CustomerNotifier notifier)
    : IDomainEventHandler<BookingConfirmedDomainEvent>
{
    public void Handle(BookingConfirmedDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var title = events.FindById(domainEvent.EventId)?.Title ?? "your event";

        notifier.Notify(
            domainEvent.CustomerId,
            $"Booking {domainEvent.Reference} confirmed",
            $"Your tickets for {title} are confirmed. Total paid: {domainEvent.Total}.");
    }
}
