using EventBooking.Domain.Events;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.Tests.TestKit;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Tests.Events;

/// <summary>
/// An event belongs to the organiser who created it. These tests exist because the rule used to live
/// only in the console screen - and a rule that lives in the user interface is not a rule.
/// </summary>
public sealed class EventOwnershipTests
{
    private static readonly UserId SomebodyElse = UserId.New();

    [Fact]
    public void AnotherOrganizerCannotPublishIt()
    {
        var concert = Given.Concert();
        concert.AddTicketType(Given.OrganizerId, "Parter", TicketTier.Standard, Money.Of(30m), 50);

        Assert.Throws<NotTheOrganizerException>(() => concert.Publish(SomebodyElse, Given.Now));
        Assert.Equal(EventStatus.Draft, concert.Status);
    }

    [Fact]
    public void AnotherOrganizerCannotCancelIt()
    {
        var concert = Given.PublishedConcert();

        Assert.Throws<NotTheOrganizerException>(() => concert.Cancel(SomebodyElse, "Ne valja mi", Given.Now));
        Assert.Equal(EventStatus.Published, concert.Status);
    }

    [Fact]
    public void AnotherOrganizerCannotRescheduleIt()
    {
        var concert = Given.PublishedConcert();
        var original = concert.Schedule;

        Assert.Throws<NotTheOrganizerException>(
            () => concert.Reschedule(SomebodyElse, Given.Schedule(90), Given.Now));

        Assert.Equal(original, concert.Schedule);
    }

    [Fact]
    public void AnotherOrganizerCannotAddTicketTypes()
    {
        var concert = Given.PublishedConcert();
        var typesBefore = concert.TicketTypes.Count;

        Assert.Throws<NotTheOrganizerException>(
            () => concert.AddTicketType(SomebodyElse, "Besplatno", TicketTier.Standard, Money.Of(0m), 500));

        Assert.Equal(typesBefore, concert.TicketTypes.Count);
    }

    [Fact]
    public void AnotherOrganizerCannotEditTheDetails()
    {
        var concert = Given.PublishedConcert();

        Assert.Throws<NotTheOrganizerException>(
            () => concert.UpdateDetails(SomebodyElse, "Preuzeto", "Preuzet opis"));

        Assert.Equal("Koncert", concert.Title);
    }

    [Fact]
    public void AnotherOrganizerCannotAddSessionsToAConference()
    {
        var conference = Given.PublishedConference();

        Assert.Throws<NotTheOrganizerException>(
            () => conference.AddSession(SomebodyElse, Given.Session(conference, "Upad", "Track A", hoursIntoEvent: 5)));
    }

    [Fact]
    public void TheOwningOrganizerCanDoAllOfIt()
    {
        var concert = Given.PublishedConcert();

        concert.AddTicketType(Given.OrganizerId, "Tribina", TicketTier.Standard, Money.Of(20m), 100);
        concert.UpdateDetails(Given.OrganizerId, "Novi naslov", "Novi opis");
        concert.Reschedule(Given.OrganizerId, Given.Schedule(90), Given.Now);
        concert.Cancel(Given.OrganizerId, "Razlog", Given.Now);

        Assert.Equal(EventStatus.Cancelled, concert.Status);
    }

    /// <summary>
    /// The one case with no person behind it: the workshop calls itself off. It must still work,
    /// which is why the aggregate keeps an unauthorised path to itself.
    /// </summary>
    [Fact]
    public void TheSystemCanStillCancelAnUndersubscribedWorkshopWithoutAnOrganizer()
    {
        var workshop = Given.PublishedWorkshop(minimumAttendees: 4, daysAhead: 20);

        workshop.RunScheduledMaintenance(workshop.Schedule.Start.AddHours(-24));

        Assert.Equal(EventStatus.Cancelled, workshop.Status);
    }
}
