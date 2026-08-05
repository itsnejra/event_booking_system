using EventBooking.Application.Abstractions;
using EventBooking.Domain.Abstractions;
using EventBooking.Domain.Events;

namespace EventBooking.Application.Notifications.Handlers;

/// <summary>
/// A new date is the one change customers must not miss, so everyone still holding tickets is told.
/// </summary>
public sealed class EventRescheduledNotificationHandler(IBookingRepository bookings, CustomerNotifier notifier)
    : IDomainEventHandler<EventRescheduledDomainEvent>
{
    public void Handle(EventRescheduledDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var affected = bookings
            .GetByEvent(domainEvent.EventId)
            .Where(booking => booking.HoldsSeats)
            .Select(booking => booking.CustomerId)
            .Distinct();

        foreach (var customerId in affected)
        {
            notifier.Notify(
                customerId,
                $"New date for {domainEvent.Title}",
                $"{domainEvent.Title} has moved from {domainEvent.PreviousSchedule} to {domainEvent.NewSchedule}. "
                + "Your tickets remain valid; cancel if the new date does not suit you.");
        }
    }
}
