using EventBooking.Application.Bookings;
using EventBooking.Application.Catalog;
using EventBooking.Domain.Entities;
using EventBooking.Domain.Enums;
using EventBooking.Domain.Interfaces;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Infrastructure.Seeding;

/// <summary>The people and events the demo starts with.</summary>
public sealed record DemoData(
    IReadOnlyList<Customer> Customers,
    IReadOnlyList<Organizer> Organizers,
    IReadOnlyList<Event> Events);

/// <summary>
/// Fills an empty system with something worth looking at.
/// </summary>
/// <remarks>
/// The seeder goes through the same services a user would, never straight into the repositories, so
/// the starting state is one the application could actually have reached. The data is chosen to make
/// each interesting rule visible: an event far enough out for early bird pricing, a workshop that
/// will not reach its minimum, a workshop that is already sold out, an unpaid hold, and a booking
/// that was cancelled and refunded.
/// </remarks>
public sealed class DemoDataSeeder(
    IVenueRepository venues,
    IUserRepository users,
    EventCatalogService catalog,
    BookingService bookings,
    IClock clock)
{
    public DemoData Seed()
    {
        var now = clock.UtcNow;
        var midnight = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);

        var (zetra, kongresniCentar, skylineHub) = SeedVenues();
        var (skylineEvents, bhKonferencije) = SeedOrganizers();
        var (lejla, emir, nina, tarik) = SeedCustomers();

        var concert = SeedConcert(zetra, skylineEvents, midnight);
        var conference = SeedConference(kongresniCentar, bhKonferencije, midnight, now);
        var undersubscribedWorkshop = SeedUndersubscribedWorkshop(skylineHub, skylineEvents, now);
        var soldOutWorkshop = SeedSoldOutWorkshop(skylineHub, skylineEvents, midnight);

        SeedBookings(concert, conference, undersubscribedWorkshop, soldOutWorkshop, lejla, emir, nina, tarik);

        return new DemoData(
            [lejla, emir, nina, tarik],
            [skylineEvents, bhKonferencije],
            [concert, conference, undersubscribedWorkshop, soldOutWorkshop]);
    }

    private (Venue Zetra, Venue KongresniCentar, Venue SkylineHub) SeedVenues()
    {
        var zetra = new Venue(VenueId.New(), "Olimpijska dvorana Zetra", "Sarajevo", 7000);
        var kongresniCentar = new Venue(VenueId.New(), "Hotel Hills - kongresni centar", "Ilidza", 900);
        var skylineHub = new Venue(VenueId.New(), "Skyline Hub", "Mostar", 60);

        venues.Add(zetra);
        venues.Add(kongresniCentar);
        venues.Add(skylineHub);

        return (zetra, kongresniCentar, skylineHub);
    }

    private (Organizer SkylineEvents, Organizer BhKonferencije) SeedOrganizers()
    {
        var skylineEvents = new Organizer(
            UserId.New(),
            "Amila Hodzic",
            EmailAddress.Create("amila.hodzic@skyline-events.ba"),
            "Skyline Events");

        var bhKonferencije = new Organizer(
            UserId.New(),
            "Damir Kovacevic",
            EmailAddress.Create("damir.kovacevic@bh-konferencije.ba"),
            "BH Konferencije");

        users.Add(skylineEvents);
        users.Add(bhKonferencije);

        return (skylineEvents, bhKonferencije);
    }

    private (Customer Lejla, Customer Emir, Customer Nina, Customer Tarik) SeedCustomers()
    {
        var lejla = new Customer(
            UserId.New(),
            "Lejla Begic",
            EmailAddress.Create("lejla.begic@example.ba"),
            MembershipTier.Gold);

        var emir = new Customer(
            UserId.New(),
            "Emir Saric",
            EmailAddress.Create("emir.saric@example.ba"),
            MembershipTier.Silver);

        var nina = new Customer(UserId.New(), "Nina Maric", EmailAddress.Create("nina.maric@example.ba"));
        var tarik = new Customer(UserId.New(), "Tarik Delic", EmailAddress.Create("tarik.delic@example.ba"));

        users.Add(lejla);
        users.Add(emir);
        users.Add(nina);
        users.Add(tarik);

        return (lejla, emir, nina, tarik);
    }

    /// <summary>Far enough out that the early bird rule still applies - 45 days.</summary>
    private ConcertEvent SeedConcert(Venue venue, Organizer organizer, DateTimeOffset midnight)
    {
        var start = midnight.AddDays(45).AddHours(20);

        var concert = catalog.CreateConcert(new CreateConcertRequest
        {
            Title = "Dubioza kolektiv - Sarajevo Live",
            Description = "Veliki koncert u Zetri, uz goste iz regije.",
            Schedule = new DateRange(start, start.AddHours(3)),
            VenueId = venue.Id,
            OrganizerId = organizer.Id,
            Headliner = "Dubioza kolektiv",
        });

        AddTicketType(concert.Id, organizer.Id, "Parter", TicketTier.Standard, 45m, 3000);
        AddTicketType(concert.Id, organizer.Id, "Tribina", TicketTier.Standard, 35m, 2500);
        AddTicketType(concert.Id, organizer.Id, "VIP loza", TicketTier.Vip, 120m, 150);

        catalog.Publish(concert.Id, organizer.Id);
        return concert;
    }

    /// <summary>Two days, three sessions across two tracks, and one batch with a closing sales window.</summary>
    private ConferenceEvent SeedConference(
        Venue venue,
        Organizer organizer,
        DateTimeOffset midnight,
        DateTimeOffset now)
    {
        var firstDay = midnight.AddDays(60).AddHours(9);
        var schedule = new DateRange(firstDay, firstDay.AddDays(1).AddHours(8));

        var conference = catalog.CreateConference(new CreateConferenceRequest
        {
            Title = ".NET Days BiH 2026",
            Description = "Dvodnevna konferencija o backend razvoju, arhitekturi i praksama testiranja.",
            Schedule = schedule,
            VenueId = venue.Id,
            OrganizerId = organizer.Id,
            Topic = "Backend i arhitektura",
        });

        conference.AddSession(organizer.Id, new ConferenceSession(
            "Modeliranje domene bez frameworka",
            "Amila Hodzic",
            "Arhitektura",
            new DateRange(firstDay.AddHours(1), firstDay.AddHours(2))));

        conference.AddSession(organizer.Id, new ConferenceSession(
            "Testovi koji prezive refaktoring",
            "Damir Kovacevic",
            "Arhitektura",
            new DateRange(firstDay.AddHours(2), firstDay.AddHours(3))));

        conference.AddSession(organizer.Id, new ConferenceSession(
            "EF Core: kada ORM radi protiv vas",
            "Nina Maric",
            "Baze podataka",
            new DateRange(firstDay.AddHours(1), firstDay.AddMinutes(150))));

        AddTicketType(conference.Id, organizer.Id, "Standard ulaznica", TicketTier.Standard, 180m, 400);
        AddTicketType(conference.Id, organizer.Id, "Studentska ulaznica", TicketTier.Student, 90m, 120);

        // A limited first batch: same event, but only on sale for the next ten days.
        AddTicketType(
            conference.Id,
            organizer.Id,
            "Prvo kolo",
            TicketTier.Standard,
            140m,
            100,
            new DateRange(now.AddDays(-5), midnight.AddDays(10)));

        catalog.Publish(conference.Id, organizer.Id);
        return conference;
    }

    /// <summary>
    /// Starts in 30 hours with a minimum of eight attendees and only two sold - the next maintenance
    /// run will call it off and refund everyone.
    /// </summary>
    private WorkshopEvent SeedUndersubscribedWorkshop(Venue venue, Organizer organizer, DateTimeOffset now)
    {
        var start = now.AddHours(30);

        var workshop = catalog.CreateWorkshop(new CreateWorkshopRequest
        {
            Title = "Domain-Driven Design u praksi",
            Description = "Cjelodnevna radionica: agregati, invarijante i granice konteksta.",
            Schedule = new DateRange(start, start.AddHours(7)),
            VenueId = venue.Id,
            OrganizerId = organizer.Id,
            Instructor = "Amila Hodzic",
            MinimumAttendees = 8,
        });

        AddTicketType(workshop.Id, organizer.Id, "Kotizacija", TicketTier.Standard, 150m, 20);

        catalog.Publish(workshop.Id, organizer.Id);
        return workshop;
    }

    /// <summary>Six seats, all of which are taken below - the starting point for the waiting list.</summary>
    private WorkshopEvent SeedSoldOutWorkshop(Venue venue, Organizer organizer, DateTimeOffset midnight)
    {
        var start = midnight.AddDays(20).AddHours(10);

        var workshop = catalog.CreateWorkshop(new CreateWorkshopRequest
        {
            Title = "Clean Architecture radionica",
            Description = "Mala grupa, rad na stvarnom kodu: slojevi, zavisnosti i testabilnost.",
            Schedule = new DateRange(start, start.AddHours(6)),
            VenueId = venue.Id,
            OrganizerId = organizer.Id,
            Instructor = "Damir Kovacevic",
            MinimumAttendees = 4,
        });

        AddTicketType(workshop.Id, organizer.Id, "Kotizacija", TicketTier.Standard, 220m, 6);

        catalog.Publish(workshop.Id, organizer.Id);
        return workshop;
    }

    private void SeedBookings(
        ConcertEvent concert,
        ConferenceEvent conference,
        WorkshopEvent undersubscribedWorkshop,
        WorkshopEvent soldOutWorkshop,
        Customer lejla,
        Customer emir,
        Customer nina,
        Customer tarik)
    {
        BookAndPay(lejla.Id, concert, "Parter", 2);
        BookAndPay(emir.Id, concert, "Tribina", 4);
        BookAndPay(nina.Id, conference, "Standard ulaznica", 1);
        BookAndPay(tarik.Id, conference, "Studentska ulaznica", 5);
        BookAndPay(nina.Id, undersubscribedWorkshop, "Kotizacija", 2);

        // Six seats, six customers' worth of demand - the workshop ends up sold out.
        BookAndPay(lejla.Id, soldOutWorkshop, "Kotizacija", 2);
        BookAndPay(emir.Id, soldOutWorkshop, "Kotizacija", 2);
        BookAndPay(tarik.Id, soldOutWorkshop, "Kotizacija", 2);

        // One booking that was paid for and then cancelled, so the reports have something to show.
        var cancelled = BookAndPay(nina.Id, concert, "Parter", 2);
        bookings.Cancel(cancelled, "Promjena planova.");

        // One hold that nobody paid for - it will lapse once the clock moves past the hold window.
        bookings.PlaceHold(lejla.Id, concert.Id, [new TicketOrderItem(TicketTypeOf(concert, "VIP loza"), 2)]);
    }

    private BookingId BookAndPay(UserId customerId, Event @event, string ticketTypeName, int quantity)
    {
        var booking = bookings.PlaceHold(
            customerId,
            @event.Id,
            [new TicketOrderItem(TicketTypeOf(@event, ticketTypeName), quantity)]);

        bookings.Confirm(booking.Id);
        return booking.Id;
    }

    private void AddTicketType(
        EventId eventId,
        UserId organizerId,
        string name,
        TicketTier tier,
        decimal price,
        int capacity,
        DateRange? salesWindow = null) =>
        catalog.AddTicketType(eventId, organizerId, new AddTicketTypeRequest
        {
            Name = name,
            Tier = tier,
            Price = Money.Of(price),
            Capacity = capacity,
            SalesWindow = salesWindow,
        });

    private static TicketTypeId TicketTypeOf(Event @event, string name) =>
        @event.FindTicketType(name)?.Id
        ?? throw new InvalidOperationException($"Seed data is inconsistent: '{@event.Title}' has no '{name}' ticket.");
}
