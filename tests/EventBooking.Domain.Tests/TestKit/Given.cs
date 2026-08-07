using EventBooking.Domain.Events;
using EventBooking.Domain.Users;
using EventBooking.Domain.ValueObjects;
using EventBooking.Domain.Venues;

namespace EventBooking.Domain.Tests.TestKit;

/// <summary>
/// Builders for the objects the tests need. They exist so that a test can say what actually matters
/// to it - "a concert 45 days out with ten VIP seats" - and stay silent about everything else.
/// </summary>
internal static class Given
{
    /// <summary>A fixed "now". Tests that involve time say how far from this point they are.</summary>
    public static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    public static readonly UserId OrganizerId = UserId.New();

    public static Venue Venue(int capacity = 5000) => new(VenueId.New(), "Dvorana", "Sarajevo", capacity);

    public static DateRange Schedule(double daysAhead, double hours = 3) =>
        DateRange.Starting(Now.AddDays(daysAhead), TimeSpan.FromHours(hours));

    public static Customer Customer(MembershipTier tier = MembershipTier.Standard, string email = "kupac@example.ba") =>
        new(UserId.New(), "Test Kupac", EmailAddress.Create(email), tier);

    public static ConcertEvent Concert(double daysAhead = 45, Venue? venue = null) => new(
        EventId.New(),
        "Koncert",
        "Opis koncerta",
        Schedule(daysAhead),
        venue ?? Venue(),
        OrganizerId,
        "Headliner");

    /// <summary>Pass <paramref name="vipSeats"/> as zero for a concert with standard tickets only.</summary>
    public static ConcertEvent PublishedConcert(int standardSeats = 100, int vipSeats = 10, double daysAhead = 45)
    {
        var concert = Concert(daysAhead);
        concert.AddTicketType(OrganizerId, "Parter", TicketTier.Standard, Money.Of(40m), standardSeats);

        if (vipSeats > 0)
        {
            concert.AddTicketType(OrganizerId, "VIP", TicketTier.Vip, Money.Of(100m), vipSeats);
        }

        concert.Publish(OrganizerId, Now);
        return concert;
    }

    public static WorkshopEvent Workshop(int minimumAttendees = 4, double daysAhead = 20, Venue? venue = null) => new(
        EventId.New(),
        "Radionica",
        "Opis radionice",
        Schedule(daysAhead, hours: 6),
        venue ?? Venue(50),
        OrganizerId,
        "Instruktor",
        minimumAttendees);

    public static WorkshopEvent PublishedWorkshop(int seats = 10, int minimumAttendees = 4, double daysAhead = 20)
    {
        var workshop = Workshop(minimumAttendees, daysAhead);
        workshop.AddTicketType(OrganizerId, "Kotizacija", TicketTier.Standard, Money.Of(200m), seats);
        workshop.Publish(OrganizerId, Now);
        return workshop;
    }

    public static ConferenceEvent Conference(double daysAhead = 60, Venue? venue = null) => new(
        EventId.New(),
        "Konferencija",
        "Opis konferencije",
        DateRange.Starting(Now.AddDays(daysAhead), TimeSpan.FromDays(2)),
        venue ?? Venue(1000),
        OrganizerId,
        "Arhitektura");

    public static ConferenceEvent PublishedConference(int seats = 200, double daysAhead = 60)
    {
        var conference = Conference(daysAhead);
        conference.AddSession(OrganizerId, Session(conference, "Uvod", "Track A", hoursIntoEvent: 1));
        conference.AddTicketType(OrganizerId, "Standard", TicketTier.Standard, Money.Of(180m), seats);
        conference.Publish(OrganizerId, Now);
        return conference;
    }

    public static ConferenceSession Session(
        ConferenceEvent conference,
        string title,
        string track,
        double hoursIntoEvent,
        double durationHours = 1) => new(
        title,
        "Predavac",
        track,
        DateRange.Starting(
            conference.Schedule.Start.AddHours(hoursIntoEvent),
            TimeSpan.FromHours(durationHours)));

    public static TicketTypeId TicketTypeOf(Event @event, string name) =>
        @event.FindTicketType(name)?.Id ?? throw new InvalidOperationException($"No ticket type '{name}'.");

    public static IReadOnlyCollection<TicketOrderItem> Order(Event @event, string ticketTypeName, int quantity) =>
        [new TicketOrderItem(TicketTypeOf(@event, ticketTypeName), quantity)];
}
