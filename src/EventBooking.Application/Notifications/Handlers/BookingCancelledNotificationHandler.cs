using EventBooking.Application.Abstractions;
using EventBooking.Domain.Abstractions;
using EventBooking.Domain.Bookings;

namespace EventBooking.Application.Notifications.Handlers;

public sealed class BookingCancelledNotificationHandler(IEventRepository events, CustomerNotifier notifier)
    : IDomainEventHandler<BookingCancelledDomainEvent>
{
    public void Handle(BookingCancelledDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var title = events.FindById(domainEvent.EventId)?.Title ?? "your event";
        var refund = domainEvent.RefundAmount.IsPositive
            ? $"A refund of {domainEvent.RefundAmount} is on its way."
            : "No refund is due for this cancellation.";

        notifier.Notify(
            domainEvent.CustomerId,
            $"Booking {domainEvent.Reference} cancelled",
            $"Your booking for {title} has been cancelled ({domainEvent.Reason}). {refund}");
    }
}
