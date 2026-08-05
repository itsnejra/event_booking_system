using EventBooking.Application.Tests.TestKit;
using EventBooking.Domain.Bookings;
using EventBooking.Domain.Events;
using EventBooking.Domain.Users;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Tests;

public sealed class ReportingServiceTests
{
    [Fact]
    public void OnAnEmptySystem_EverythingIsZero()
    {
        using var host = new TestHost();

        var summary = host.Reporting.Summary();

        Assert.Equal(0, summary.TotalEvents);
        Assert.True(summary.NetRevenue.IsZero);
    }

    [Fact]
    public void UnpaidHoldsDoNotCountAsRevenue()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 50);
        host.Bookings.PlaceHold(host.AddCustomer().Id, concert.Id, TestHost.Order(concert, "Parter", 2));

        var summary = host.Reporting.Summary();

        Assert.Equal(1, summary.ActiveHolds);
        Assert.Equal(0, summary.TicketsSold);
        Assert.True(summary.NetRevenue.IsZero);
    }

    [Fact]
    public void GrossCountsWhatCameIn_AndRefundsAreShownSeparately()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 50, daysAhead: 45);
        var kept = BookAndPay(host, concert, 2);
        var returned = BookAndPay(host, concert, 2);
        host.Bookings.Cancel(returned.Id, "Promjena planova");

        var report = host.Reporting.ForEvent(concert.Id);

        Assert.Equal(kept.Total + returned.Total, report.GrossRevenue);
        Assert.Equal(returned.Total, report.Refunds);
        Assert.Equal(kept.Total, report.NetRevenue);
        Assert.Equal(1, report.Cancellations);
        Assert.Equal(2, report.Bookings);
    }

    [Fact]
    public void OccupancyReflectsSeatsThatWereActuallySold()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 20);
        BookAndPay(host, concert, 5);

        var report = host.Reporting.ForEvent(concert.Id);

        Assert.Equal(5, report.TicketsSold);
        Assert.Equal(20, report.Capacity);
        Assert.Equal(Percentage.Of(25m), report.Occupancy);
    }

    [Fact]
    public void RevenueByCategory_GroupsEventsOfTheSameKind()
    {
        using var host = new TestHost();
        var firstConcert = host.PublishConcert(seats: 50, daysAhead: 45);
        var secondConcert = host.PublishConcert(seats: 50, daysAhead: 45);
        var workshop = host.PublishWorkshop(seats: 10);

        BookAndPay(host, firstConcert, 2);
        BookAndPay(host, secondConcert, 2);
        BookAndPay(host, workshop, 2, ticketTypeName: "Kotizacija");

        var lines = host.Reporting.RevenueByCategory();

        var concerts = Assert.Single(lines, line => line.Category == EventCategory.Concert);
        Assert.Equal(2, concerts.Events);
        Assert.Equal(4, concerts.TicketsSold);

        var workshops = Assert.Single(lines, line => line.Category == EventCategory.Workshop);
        Assert.Equal(1, workshops.Events);
    }

    [Fact]
    public void TopCustomers_AreRankedBySpend()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 50, daysAhead: 5);
        var bigSpender = host.AddCustomer(name: "Veliki");
        var smallSpender = host.AddCustomer(name: "Mali");

        PayAs(host, concert, bigSpender, 4);
        PayAs(host, concert, smallSpender, 1);

        var top = host.Reporting.TopCustomers();

        Assert.Equal("Veliki", top[0].Customer.FullName);
        Assert.Equal(4, top[0].TicketsBought);
        Assert.Equal(Money.Of(160m), top[0].TotalSpend);
        Assert.Equal("Mali", top[1].Customer.FullName);
    }

    [Fact]
    public void TopCustomers_IgnoresPeopleWhoNeverPaid()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 50);
        var browser = host.AddCustomer(name: "Samo gleda");
        host.Bookings.PlaceHold(browser.Id, concert.Id, TestHost.Order(concert, "Parter", 1));

        Assert.Empty(host.Reporting.TopCustomers());
    }

    [Fact]
    public void TopCustomers_RespectsTheRequestedSize()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 50, daysAhead: 5);

        for (var index = 0; index < 4; index++)
        {
            PayAs(host, concert, host.AddCustomer(name: $"Kupac {index}"), index + 1);
        }

        Assert.Equal(2, host.Reporting.TopCustomers(take: 2).Count);
    }

    [Fact]
    public void Summary_CountsSoldOutEvents()
    {
        using var host = new TestHost();
        var soldOut = host.PublishConcert(seats: 2);
        host.PublishConcert(seats: 50);
        BookAndPay(host, soldOut, 2);

        var summary = host.Reporting.Summary();

        Assert.Equal(2, summary.TotalEvents);
        Assert.Equal(1, summary.SoldOutEvents);
        Assert.Equal(2, summary.TicketsSold);
    }

    [Fact]
    public void EventPerformance_IsOrderedByNetRevenue()
    {
        using var host = new TestHost();
        var quiet = host.PublishConcert(seats: 50, daysAhead: 5);
        var busy = host.PublishConcert(seats: 50, daysAhead: 5);
        BookAndPay(host, quiet, 1);
        BookAndPay(host, busy, 5);

        var reports = host.Reporting.EventPerformance();

        Assert.Equal(busy, reports[0].Event);
        Assert.Equal(quiet, reports[1].Event);
    }

    private static Booking BookAndPay(
        TestHost host,
        Event @event,
        int quantity,
        string ticketTypeName = "Parter") =>
        PayAs(host, @event, host.AddCustomer(), quantity, ticketTypeName);

    private static Booking PayAs(
        TestHost host,
        Event @event,
        Customer customer,
        int quantity,
        string ticketTypeName = "Parter")
    {
        var booking = host.Bookings.PlaceHold(
            customer.Id,
            @event.Id,
            TestHost.Order(@event, ticketTypeName, quantity));

        host.Bookings.Confirm(booking.Id);
        return booking;
    }
}
