using EventBooking.Domain.Events;
using EventBooking.Domain.Users;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Reporting;

// Read models. They are records with no behaviour on purpose: a report is a snapshot of an answer,
// and giving it methods would invite business logic to move out of the domain and into the reports.

/// <summary>How one event is doing: how full it is and what it has actually earned.</summary>
public sealed record EventPerformanceReport(
    Event Event,
    int Capacity,
    int TicketsSold,
    Percentage Occupancy,
    int Bookings,
    int Cancellations,
    Money GrossRevenue,
    Money Refunds)
{
    public Money NetRevenue => GrossRevenue.SubtractOrZero(Refunds);
}

public sealed record CategoryRevenueLine(
    EventCategory Category,
    int Events,
    int TicketsSold,
    Money NetRevenue);

public sealed record CustomerActivityLine(
    Customer Customer,
    int Bookings,
    int TicketsBought,
    Money TotalSpend);

/// <summary>The one-screen answer to "how is the platform doing?".</summary>
public sealed record PlatformSummary(
    int TotalEvents,
    int PublishedEvents,
    int SoldOutEvents,
    int TotalBookings,
    int ActiveHolds,
    int TicketsSold,
    Money NetRevenue);
