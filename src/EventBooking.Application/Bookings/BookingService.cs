using EventBooking.Application.Abstractions;
using EventBooking.Domain.Entities;
using EventBooking.Domain.Enums;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.Interfaces;
using EventBooking.Domain.Pricing;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Bookings;

/// <summary>
/// Runs the booking flow end to end: hold seats, price them, take payment, give them back.
/// </summary>
/// <remarks>
/// A booking touches two aggregates - the event owns the seats, the booking owns the money - and
/// this service is the only place where the two move together. That is a deliberate trade-off: with
/// a real database each method here would be one transaction, and keeping the coordination in one
/// class is what makes that change a local one.
/// </remarks>
public sealed class BookingService(
    IEventRepository events,
    IBookingRepository bookings,
    IUserRepository users,
    PricingEngine pricingEngine,
    IBookingReferenceGenerator references,
    IDomainEventDispatcher dispatcher,
    IClock clock)
{
    /// <summary>
    /// Holds the requested seats and works out what they cost. The seats are gone from the pool from
    /// this moment, but the customer has not paid yet - see <see cref="Confirm"/> and
    /// <see cref="ExpireStaleHolds"/>.
    /// </summary>
    public Booking PlaceHold(UserId customerId, EventId eventId, IReadOnlyCollection<TicketOrderItem> items)
    {
        var now = clock.UtcNow;
        var customer = GetCustomer(customerId);
        var @event = GetEvent(eventId);

        var reservation = @event.Reserve(items, now);

        Booking booking;
        try
        {
            var pricedOrder = pricingEngine.Price(@event, customer, reservation, now);
            booking = Booking.PlaceHold(BookingId.New(), references.NextReference(), customerId, pricedOrder, now);
            bookings.Add(booking);
        }
        catch
        {
            // Nothing downstream of Reserve may leave seats stranded. In a database-backed version
            // the surrounding transaction would do this for us.
            @event.ReleaseReservation(reservation, wasPaidFor: false, now);
            dispatcher.Dispatch(@event);
            throw;
        }

        dispatcher.Dispatch(@event, booking);
        return booking;
    }

    /// <summary>Takes the payment: the held seats become sold and the customer's loyalty counter moves.</summary>
    public Booking Confirm(BookingId bookingId)
    {
        var now = clock.UtcNow;
        var booking = GetBooking(bookingId);
        var @event = GetEvent(booking.EventId);

        if (@event.Status == EventStatus.Cancelled)
        {
            throw new BusinessRuleViolationException(
                $"'{@event.Title}' has been cancelled; this booking cannot be paid for.");
        }

        booking.Confirm(now);
        @event.ConfirmReservation(booking.AsReservation());
        GetCustomer(booking.CustomerId).RegisterCompletedBooking();

        dispatcher.Dispatch(@event, booking);
        return booking;
    }

    /// <summary>Cancels at the customer's request; the refund follows the event's own policy.</summary>
    public Booking Cancel(BookingId bookingId, string reason)
    {
        var now = clock.UtcNow;
        var booking = GetBooking(bookingId);
        var @event = GetEvent(booking.EventId);

        var wasPaidFor = booking.IsPaidFor;
        booking.Cancel(@event.RefundPolicy, @event.Schedule.Start, now, reason);
        @event.ReleaseReservation(booking.AsReservation(), wasPaidFor, now);

        dispatcher.Dispatch(@event, booking);
        return booking;
    }

    /// <summary>
    /// Cancels everything still outstanding for an event, with a full refund. Used when the organiser
    /// calls the event off, or when a workshop fails to reach its minimum.
    /// </summary>
    public IReadOnlyList<Booking> CancelAllForEvent(Event @event, string reason)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var now = clock.UtcNow;
        var affected = bookings.GetByEvent(@event.Id).Where(booking => booking.HoldsSeats).ToList();

        foreach (var booking in affected)
        {
            // The seats go back for the same reason they do when a customer cancels: nobody holds
            // them any more. Without this the event keeps reporting them as sold long after every
            // booking was refunded. The waiting list is not disturbed - its handler only speaks for
            // events that are still published.
            var wasPaidFor = booking.IsPaidFor;
            booking.CancelBecauseEventCancelled(now, reason);
            @event.ReleaseReservation(booking.AsReservation(), wasPaidFor, now);

            dispatcher.Dispatch(@event, booking);
        }

        return affected;
    }

    /// <summary>
    /// Returns the seats of every hold whose clock ran out. This is the sweep a scheduled job would
    /// run; the console exposes it as a menu item so the behaviour can be seen rather than described.
    /// </summary>
    public int ExpireStaleHolds()
    {
        var now = clock.UtcNow;
        var expired = 0;

        foreach (var booking in bookings.GetExpiredHolds(now))
        {
            if (!booking.Expire(now))
            {
                continue;
            }

            var @event = GetEvent(booking.EventId);
            @event.ReleaseReservation(booking.AsReservation(), wasPaidFor: false, now);

            dispatcher.Dispatch(@event, booking);
            expired++;
        }

        return expired;
    }

    public WaitlistEntry JoinWaitlist(UserId customerId, EventId eventId, int quantity)
    {
        var customer = GetCustomer(customerId);
        var @event = GetEvent(eventId);

        var entry = @event.JoinWaitlist(customer.Id, quantity, clock.UtcNow);
        dispatcher.Dispatch(@event);
        return entry;
    }

    public IReadOnlyList<Booking> GetForCustomer(UserId customerId) =>
        [.. bookings.GetByCustomer(customerId).OrderByDescending(booking => booking.CreatedAt)];

    public Booking GetById(BookingId bookingId) => GetBooking(bookingId);

    public Booking GetByReference(string reference)
    {
        var parsed = BookingReference.Create(reference);
        return bookings.FindByReference(parsed) ?? throw EntityNotFoundException.For<Booking>(parsed);
    }

    private Booking GetBooking(BookingId bookingId) =>
        bookings.FindById(bookingId) ?? throw EntityNotFoundException.For<Booking>(bookingId);

    private Event GetEvent(EventId eventId) =>
        events.FindById(eventId) ?? throw EntityNotFoundException.For<Event>(eventId);

    private Customer GetCustomer(UserId customerId) =>
        users.FindById(customerId) as Customer ?? throw EntityNotFoundException.For<Customer>(customerId);
}
