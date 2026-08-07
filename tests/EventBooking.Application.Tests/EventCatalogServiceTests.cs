using EventBooking.Application.Catalog;
using EventBooking.Application.Tests.TestKit;
using EventBooking.Domain.Bookings;
using EventBooking.Domain.Events;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Tests;

public sealed class EventCatalogServiceTests
{
    [Fact]
    public void CreateConcert_StoresADraft()
    {
        using var host = new TestHost();

        var concert = host.Catalog.CreateConcert(NewConcertRequest(host));

        Assert.Equal(EventStatus.Draft, concert.Status);
        Assert.Equal(concert, host.Catalog.GetById(concert.Id));
    }

    [Fact]
    public void Create_WithAVenueThatDoesNotExist_Throws()
    {
        using var host = new TestHost();
        var request = NewConcertRequest(host) with { VenueId = VenueId.New() };

        Assert.Throws<EntityNotFoundException>(() => host.Catalog.CreateConcert(request));
    }

    /// <summary>Creating events is an organiser's job; who is calling is a decision for this layer.</summary>
    [Fact]
    public void Create_WithACustomerAsTheOrganizer_Throws()
    {
        using var host = new TestHost();
        var request = NewConcertRequest(host) with { OrganizerId = host.AddCustomer().Id };

        Assert.Throws<EntityNotFoundException>(() => host.Catalog.CreateConcert(request));
    }

    [Fact]
    public void GetByOrganizer_ReturnsOnlyThatOrganizersEvents()
    {
        using var host = new TestHost();
        var first = host.PublishConcert();
        host.PublishConcert();

        var theirs = host.Catalog.GetByOrganizer(first.OrganizerId);

        Assert.Equal(first, Assert.Single(theirs));
    }

    [Fact]
    public void Cancel_CancelsEveryOutstandingBookingAndRefundsInFull()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 20, daysAhead: 45);
        var paid = BookAndPay(host, concert, 2);
        var held = host.Bookings.PlaceHold(host.AddCustomer(name: "Drugi").Id, concert.Id, TestHost.Order(concert, "Parter", 1));

        // A day before the concert the normal policy would refund nothing at all.
        host.Clock.MoveTo(concert.Schedule.Start.AddDays(-1));
        var affected = host.Catalog.Cancel(concert.Id, concert.OrganizerId, "Bolest izvodjaca");

        Assert.Equal(2, affected);
        Assert.Equal(EventStatus.Cancelled, concert.Status);
        Assert.Equal(BookingStatus.Cancelled, paid.Status);
        Assert.Equal(paid.Total, paid.RefundAmount);
        Assert.Equal(BookingStatus.Cancelled, held.Status);
        Assert.True(held.RefundAmount is { IsZero: true });
    }

    [Fact]
    public void Cancel_LeavesAlreadyCancelledBookingsAlone()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 20, daysAhead: 45);
        var booking = BookAndPay(host, concert, 1);
        host.Bookings.Cancel(booking.Id, "Vec otkazano");

        var affected = host.Catalog.Cancel(concert.Id, concert.OrganizerId, "Otkazano");

        Assert.Equal(0, affected);
    }

    [Fact]
    public void Reschedule_KeepsTheBookingsIntact()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 20, daysAhead: 45);
        var booking = BookAndPay(host, concert, 2);

        host.Catalog.Reschedule(concert.Id, concert.OrganizerId, host.Schedule(daysAhead: 90));

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(2, concert.TicketsSold);
    }

    [Fact]
    public void AddTicketType_AfterPublishing_IsAllowed()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 10);

        host.AddTicketType(concert.Id, "Tribina", TicketTier.Standard, 25m, 30);

        Assert.Equal(40, concert.TotalCapacity);
        Assert.Equal(Money.Of(25m), concert.CheapestTicketPrice);
    }

    private static Booking BookAndPay(TestHost host, Event @event, int quantity)
    {
        var booking = host.Bookings.PlaceHold(
            host.AddCustomer().Id,
            @event.Id,
            TestHost.Order(@event, "Parter", quantity));

        host.Bookings.Confirm(booking.Id);
        return booking;
    }

    private static CreateConcertRequest NewConcertRequest(TestHost host) => new()
    {
        Title = "Koncert",
        Description = "Opis",
        Schedule = host.Schedule(daysAhead: 30),
        VenueId = host.AddVenue().Id,
        OrganizerId = host.AddOrganizer().Id,
        Headliner = "Headliner",
    };
}
