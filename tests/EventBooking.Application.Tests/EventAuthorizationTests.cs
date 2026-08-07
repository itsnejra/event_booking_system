using EventBooking.Application.Catalog;
using EventBooking.Application.Tests.TestKit;
using EventBooking.Domain.Events;
using EventBooking.Domain.Exceptions;

namespace EventBooking.Application.Tests;

/// <summary>
/// The same ownership rule, seen from the service. It is checked in the aggregate, so the service
/// cannot let it through by forgetting to ask.
/// </summary>
public sealed class EventAuthorizationTests
{
    [Fact]
    public void AnotherOrganizerCannotPublishSomebodyElsesEvent()
    {
        using var host = new TestHost();
        var concert = host.Catalog.CreateConcert(NewConcertRequest(host));
        host.AddTicketType(concert.Id, "Parter", TicketTier.Standard, 40m, 100);
        var intruder = host.AddOrganizer();

        Assert.Throws<NotTheOrganizerException>(() => host.Catalog.Publish(concert.Id, intruder.Id));
        Assert.Equal(EventStatus.Draft, concert.Status);
    }

    [Fact]
    public void AnotherOrganizerCannotCancelSomebodyElsesEvent()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 20);
        var intruder = host.AddOrganizer();

        Assert.Throws<NotTheOrganizerException>(
            () => host.Catalog.Cancel(concert.Id, intruder.Id, "Ne svidja mi se"));

        Assert.Equal(EventStatus.Published, concert.Status);
    }

    [Fact]
    public void AnotherOrganizerCannotAddTicketTypesToSomebodyElsesEvent()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 20);
        var intruder = host.AddOrganizer();

        Assert.Throws<NotTheOrganizerException>(() => host.Catalog.AddTicketType(
            concert.Id,
            intruder.Id,
            new AddTicketTypeRequest
            {
                Name = "Ubaceno",
                Tier = TicketTier.Standard,
                Price = Domain.ValueObjects.Money.Of(1m),
                Capacity = 10,
            }));
    }

    [Fact]
    public void AFailedCancellationLeavesTheBookingsUntouched()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 20, daysAhead: 45);
        var booking = host.Bookings.PlaceHold(
            host.AddCustomer().Id,
            concert.Id,
            TestHost.Order(concert, "Parter", 2));
        host.Bookings.Confirm(booking.Id);

        var intruder = host.AddOrganizer();

        Assert.Throws<NotTheOrganizerException>(
            () => host.Catalog.Cancel(concert.Id, intruder.Id, "Otkazujem tudje"));

        Assert.Equal(Domain.Bookings.BookingStatus.Confirmed, booking.Status);
        Assert.Equal(2, concert.TicketsSold);
    }

    [Fact]
    public void TheOwningOrganizerIsStillAllowedThrough()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 20);

        host.Catalog.Reschedule(concert.Id, concert.OrganizerId, host.Schedule(daysAhead: 120));

        Assert.Equal(EventStatus.Published, concert.Status);
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
