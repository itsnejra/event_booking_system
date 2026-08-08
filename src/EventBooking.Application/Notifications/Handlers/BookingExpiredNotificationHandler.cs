using EventBooking.Application.Abstractions;
using EventBooking.Domain.DomainEvents;
using EventBooking.Domain.Entities;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Notifications.Handlers;

public sealed class BookingExpiredNotificationHandler(CustomerNotifier notifier)
    : IDomainEventHandler<BookingExpiredDomainEvent>
{
    public void Handle(BookingExpiredDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        notifier.Notify(
            domainEvent.CustomerId,
            $"Booking {domainEvent.Reference} expired",
            "We held your seats but did not receive payment in time, so they have been released.");
    }
}
