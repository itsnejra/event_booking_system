using EventBooking.Application.Tests.TestKit;
using EventBooking.Domain.Bookings;
using EventBooking.Domain.Events;

namespace EventBooking.Application.Tests;

public sealed class MaintenanceServiceTests
{
    [Fact]
    public void OnAQuietSystem_NothingHappens()
    {
        using var host = new TestHost();
        host.PublishConcert();

        var summary = host.Maintenance.Run();

        Assert.False(summary.DidSomething);
    }

    [Fact]
    public void ExpiredHoldsAreSweptUp()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 10);
        host.Bookings.PlaceHold(host.AddCustomer().Id, concert.Id, TestHost.Order(concert, "Parter", 3));

        host.Clock.Advance(Booking.DefaultHoldDuration);
        var summary = host.Maintenance.Run();

        Assert.Equal(1, summary.ExpiredHolds);
        Assert.Equal(10, concert.AvailableTickets);
    }

    [Fact]
    public void EventsThatAreOverAreClosed()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(daysAhead: 10);

        host.Clock.MoveTo(concert.Schedule.End.AddHours(1));
        var summary = host.Maintenance.Run();

        Assert.Equal(1, summary.CompletedEvents);
        Assert.Equal(EventStatus.Completed, concert.Status);
    }

    /// <summary>
    /// The whole chain in one test: the workshop decides for itself that it cannot run, the service
    /// notices the status change, and every affected booking is cancelled and refunded in full.
    /// </summary>
    [Fact]
    public void AWorkshopThatDidNotFillUpCancelsItselfAndRefundsEveryone()
    {
        using var host = new TestHost();
        var workshop = host.PublishWorkshop(seats: 10, minimumAttendees: 6, daysAhead: 20);
        var booking = host.Bookings.PlaceHold(
            host.AddCustomer().Id,
            workshop.Id,
            TestHost.Order(workshop, "Kotizacija", 2));
        host.Bookings.Confirm(booking.Id);

        host.Clock.MoveTo(workshop.Schedule.Start.AddHours(-24));
        var summary = host.Maintenance.Run();

        Assert.Equal(1, summary.CancelledEvents);
        Assert.Equal(1, summary.RefundedBookings);
        Assert.Equal(EventStatus.Cancelled, workshop.Status);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(booking.Total, booking.RefundAmount);
    }

    [Fact]
    public void AWorkshopThatReachedItsMinimumIsLeftAlone()
    {
        using var host = new TestHost();
        var workshop = host.PublishWorkshop(seats: 10, minimumAttendees: 2, daysAhead: 20);
        var booking = host.Bookings.PlaceHold(
            host.AddCustomer().Id,
            workshop.Id,
            TestHost.Order(workshop, "Kotizacija", 2));
        host.Bookings.Confirm(booking.Id);

        host.Clock.MoveTo(workshop.Schedule.Start.AddHours(-24));
        host.Maintenance.Run();

        Assert.Equal(EventStatus.Published, workshop.Status);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void RunningTwiceDoesNotRepeatTheWork()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(daysAhead: 10);
        host.Clock.MoveTo(concert.Schedule.End.AddHours(1));

        host.Maintenance.Run();
        var second = host.Maintenance.Run();

        Assert.False(second.DidSomething);
    }
}
