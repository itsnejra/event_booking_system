using EventBooking.Domain.Abstractions;
using EventBooking.Domain.Bookings;
using EventBooking.Domain.Events;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.Users;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Reporting;

/// <summary>
/// Answers the questions an organiser actually asks. Reporting is read-only and derives everything
/// from the aggregates, so there is no separate set of totals that can quietly disagree with them.
/// </summary>
/// <remarks>
/// Revenue counts every booking that was ever paid for, and subtracts what was refunded. A booking
/// that was confirmed and later cancelled therefore shows up in both columns, which is the honest
/// picture: the money did come in, and some of it did go back out.
/// </remarks>
public sealed class ReportingService(
    IEventRepository events,
    IBookingRepository bookings,
    IUserRepository users,
    ReportingOptions options)
{
    public EventPerformanceReport ForEvent(EventId eventId)
    {
        var @event = events.FindById(eventId) ?? throw EntityNotFoundException.For<Event>(eventId);
        return Build(@event, bookings.GetByEvent(eventId));
    }

    public IReadOnlyList<EventPerformanceReport> EventPerformance() =>
    [
        .. events.GetAll()
            .Select(@event => Build(@event, bookings.GetByEvent(@event.Id)))
            .OrderByDescending(report => report.NetRevenue.Amount)
    ];

    public IReadOnlyList<CategoryRevenueLine> RevenueByCategory() =>
    [
        .. EventPerformance()
            .Where(report => report.Event.Currency == options.Currency)
            .GroupBy(report => report.Event.Category)
            .Select(group => new CategoryRevenueLine(
                group.Key,
                group.Count(),
                group.Sum(report => report.TicketsSold),
                Money.Sum(group.Select(report => report.NetRevenue), options.Currency)))
            .OrderByDescending(line => line.NetRevenue.Amount)
    ];

    public IReadOnlyList<CustomerActivityLine> TopCustomers(int take = 5)
    {
        var spendByCustomer = bookings.GetAll()
            .Where(booking => booking.ConfirmedAt is not null && booking.Currency == options.Currency)
            .GroupBy(booking => booking.CustomerId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return
        [
            .. users.GetCustomers()
                .Where(customer => spendByCustomer.ContainsKey(customer.Id))
                .Select(customer => ToActivityLine(customer, spendByCustomer[customer.Id]))
                .OrderByDescending(line => line.TotalSpend.Amount)
                .Take(take)
        ];
    }

    public PlatformSummary Summary()
    {
        var allEvents = events.GetAll();
        var allBookings = bookings.GetAll();
        var relevant = allBookings.Where(booking => booking.Currency == options.Currency).ToList();

        var gross = Money.Sum(
            relevant.Where(booking => booking.ConfirmedAt is not null).Select(booking => booking.Total),
            options.Currency);

        var refunds = Money.Sum(
            relevant.Select(booking => booking.RefundAmount ?? Money.Zero(options.Currency)),
            options.Currency);

        return new PlatformSummary(
            allEvents.Count,
            allEvents.Count(@event => @event.Status == EventStatus.Published),
            allEvents.Count(@event => @event.IsSoldOut),
            allBookings.Count,
            allBookings.Count(booking => booking.Status == BookingStatus.Pending),
            allEvents.Sum(@event => @event.TicketsSold),
            gross.SubtractOrZero(refunds));
    }

    private static EventPerformanceReport Build(Event @event, IReadOnlyCollection<Booking> eventBookings)
    {
        var currency = @event.Currency;

        var gross = Money.Sum(
            eventBookings.Where(booking => booking.ConfirmedAt is not null).Select(booking => booking.Total),
            currency);

        var refunds = Money.Sum(
            eventBookings.Select(booking => booking.RefundAmount ?? Money.Zero(currency)),
            currency);

        return new EventPerformanceReport(
            @event,
            @event.TotalCapacity,
            @event.TicketsSold,
            @event.OccupancyRate,
            eventBookings.Count,
            eventBookings.Count(booking => booking.Status == BookingStatus.Cancelled),
            gross,
            refunds);
    }

    /// <summary>
    /// Spend and tickets are counted the same way the per-event report counts them: a cancellation
    /// gives back whatever the refund policy returned, and the seats go with it. What the policy did
    /// not return is still spend, so a partial refund leaves the retained part standing. The booking
    /// count stays whole - it is how many times this person bought, cancellations included, which is
    /// what the Cancelled column of the per-event report also does.
    /// </summary>
    private CustomerActivityLine ToActivityLine(Customer customer, List<Booking> customerBookings) =>
        new(
            customer,
            customerBookings.Count,
            customerBookings.Where(booking => booking.IsPaidFor).Sum(booking => booking.TotalTickets),
            Money.Sum(
                customerBookings.Select(booking =>
                    booking.Total.SubtractOrZero(booking.RefundAmount ?? Money.Zero(options.Currency))),
                options.Currency));
}
