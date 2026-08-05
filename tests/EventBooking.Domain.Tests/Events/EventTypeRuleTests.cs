using EventBooking.Domain.Events;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.Tests.TestKit;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Tests.Events;

/// <summary>
/// The rules that actually differ between the three kinds of event. If these pass, the inheritance
/// is earning its keep; if they could be deleted, the subclasses would be data and not behaviour.
/// </summary>
public sealed class EventTypeRuleTests
{
    [Theory]
    [InlineData(typeof(ConcertEvent), 6)]
    [InlineData(typeof(ConferenceEvent), 20)]
    [InlineData(typeof(WorkshopEvent), 2)]
    public void EachEventTypeSetsItsOwnBookingLimit(Type eventType, int expectedLimit)
    {
        Event @event = eventType switch
        {
            _ when eventType == typeof(ConcertEvent) => Given.Concert(),
            _ when eventType == typeof(ConferenceEvent) => Given.Conference(),
            _ => Given.Workshop(),
        };

        Assert.Equal(expectedLimit, @event.MaxTicketsPerBooking);
    }

    // --- concert ---------------------------------------------------------------------------------

    [Fact]
    public void Concert_RefusesMoreThanTwoVipTicketsInOneBooking()
    {
        var concert = Given.PublishedConcert(vipSeats: 20);

        var exception = Assert.Throws<BusinessRuleViolationException>(
            () => concert.Reserve(Given.Order(concert, "VIP", 3), Given.Now));

        Assert.Contains("VIP", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Concert_AllowsTwoVipTicketsAlongsideStandardOnes()
    {
        var concert = Given.PublishedConcert(vipSeats: 20);

        var reservation = concert.Reserve(
            [
                new TicketOrderItem(Given.TicketTypeOf(concert, "VIP"), 2),
                new TicketOrderItem(Given.TicketTypeOf(concert, "Parter"), 4),
            ],
            Given.Now);

        Assert.Equal(6, reservation.TotalQuantity);
    }

    [Fact]
    public void Concert_RefusesMoreThanSixTicketsInTotal()
    {
        var concert = Given.PublishedConcert();

        Assert.Throws<BusinessRuleViolationException>(
            () => concert.Reserve(Given.Order(concert, "Parter", 7), Given.Now));
    }

    [Fact]
    public void Concert_CannotGoOnSaleWithVipTicketsOnly()
    {
        var concert = Given.Concert();
        concert.AddTicketType("VIP", TicketTier.Vip, Money.Of(120m), 50);

        Assert.Throws<BusinessRuleViolationException>(() => concert.Publish(Given.Now));
    }

    // --- conference ------------------------------------------------------------------------------

    [Fact]
    public void Conference_CannotGoOnSaleWithoutAProgramme()
    {
        var conference = Given.Conference();
        conference.AddTicketType("Standard", TicketTier.Standard, Money.Of(180m), 100);

        Assert.Throws<BusinessRuleViolationException>(() => conference.Publish(Given.Now));
    }

    [Fact]
    public void Conference_RefusesTwoTalksOnOneTrackAtTheSameTime()
    {
        var conference = Given.Conference();
        conference.AddSession(Given.Session(conference, "Prvi", "Arhitektura", hoursIntoEvent: 2));

        Assert.Throws<BusinessRuleViolationException>(
            () => conference.AddSession(Given.Session(conference, "Drugi", "Arhitektura", hoursIntoEvent: 2.5)));
    }

    [Fact]
    public void Conference_AllowsParallelTalksOnDifferentTracks()
    {
        var conference = Given.Conference();
        conference.AddSession(Given.Session(conference, "Prvi", "Arhitektura", hoursIntoEvent: 2));
        conference.AddSession(Given.Session(conference, "Drugi", "Baze", hoursIntoEvent: 2));

        Assert.Equal(2, conference.Sessions.Count);
        Assert.Equal(2, conference.Tracks.Count);
    }

    [Fact]
    public void Conference_RefusesATalkOutsideItsOwnSchedule()
    {
        var conference = Given.Conference();

        Assert.Throws<BusinessRuleViolationException>(
            () => conference.AddSession(Given.Session(conference, "Prerano", "Arhitektura", hoursIntoEvent: -5)));
    }

    [Fact]
    public void Conference_AllowsGroupBookingsOfTwentyTickets()
    {
        var conference = Given.PublishedConference();

        var reservation = conference.Reserve(Given.Order(conference, "Standard", 20), Given.Now);

        Assert.Equal(20, reservation.TotalQuantity);
    }

    // --- workshop --------------------------------------------------------------------------------

    [Fact]
    public void Workshop_RefusesMoreThanTwoTicketsInOneBooking()
    {
        var workshop = Given.PublishedWorkshop();

        Assert.Throws<BusinessRuleViolationException>(
            () => workshop.Reserve(Given.Order(workshop, "Kotizacija", 3), Given.Now));
    }

    [Fact]
    public void Workshop_CannotGoOnSaleWhenItsMinimumExceedsItsSeats()
    {
        var workshop = Given.Workshop(minimumAttendees: 20);
        workshop.AddTicketType("Kotizacija", TicketTier.Standard, Money.Of(200m), 10);

        Assert.Throws<BusinessRuleViolationException>(() => workshop.Publish(Given.Now));
    }

    [Fact]
    public void Workshop_ReportsHowManySeatsItStillNeeds()
    {
        var workshop = Given.PublishedWorkshop(minimumAttendees: 4);
        workshop.ConfirmReservation(workshop.Reserve(Given.Order(workshop, "Kotizacija", 1), Given.Now));

        Assert.False(workshop.IsViable);
        Assert.Equal(3, workshop.SeatsUntilViable);
    }

    [Fact]
    public void Workshop_CancelsItselfWhenTheDeadlinePassesWithoutEnoughAttendees()
    {
        var workshop = Given.PublishedWorkshop(minimumAttendees: 4, daysAhead: 20);
        workshop.ConfirmReservation(workshop.Reserve(Given.Order(workshop, "Kotizacija", 2), Given.Now));

        workshop.RunScheduledMaintenance(workshop.Schedule.Start.AddHours(-24));

        Assert.Equal(EventStatus.Cancelled, workshop.Status);
        Assert.Contains(workshop.DomainEvents, domainEvent => domainEvent is EventCancelledDomainEvent);
    }

    [Fact]
    public void Workshop_SurvivesTheDeadlineOnceItHasEnoughAttendees()
    {
        var workshop = Given.PublishedWorkshop(minimumAttendees: 2, daysAhead: 20);
        workshop.ConfirmReservation(workshop.Reserve(Given.Order(workshop, "Kotizacija", 2), Given.Now));

        workshop.RunScheduledMaintenance(workshop.Schedule.Start.AddHours(-24));

        Assert.Equal(EventStatus.Published, workshop.Status);
    }

    [Fact]
    public void Workshop_IsLeftAloneWhileTheDeadlineIsStillAway()
    {
        var workshop = Given.PublishedWorkshop(minimumAttendees: 4, daysAhead: 20);

        workshop.RunScheduledMaintenance(Given.Now.AddDays(5));

        Assert.Equal(EventStatus.Published, workshop.Status);
    }
}
