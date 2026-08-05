using EventBooking.Application.Bookings;
using EventBooking.Application.Catalog;
using EventBooking.Application.DependencyInjection;
using EventBooking.Application.Maintenance;
using EventBooking.Application.Reporting;
using EventBooking.Domain.Abstractions;
using EventBooking.Domain.Events;
using EventBooking.Domain.Users;
using EventBooking.Domain.ValueObjects;
using EventBooking.Domain.Venues;
using EventBooking.Infrastructure.DependencyInjection;
using EventBooking.Infrastructure.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace EventBooking.Application.Tests.TestKit;

/// <summary>
/// A whole system in a variable: real services, real domain, in-memory stores and a clock the test
/// controls. Nothing is mocked, because the interesting behaviour of this layer <em>is</em> the way
/// the pieces move together - a mock would only assert that the test knows its own wiring.
/// </summary>
internal sealed class TestHost : IDisposable
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider _provider;
    private int _emailCounter;

    public TestHost(DateTimeOffset? now = null)
    {
        Clock = new FakeClock(now ?? DefaultNow);

        var services = new ServiceCollection();
        services.AddEventBookingApplication();
        services.AddEventBookingInfrastructure();
        services.AddSingleton<IClock>(Clock);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    public FakeClock Clock { get; }

    public EventCatalogService Catalog => Resolve<EventCatalogService>();

    public BookingService Bookings => Resolve<BookingService>();

    public ReportingService Reporting => Resolve<ReportingService>();

    public MaintenanceService Maintenance => Resolve<MaintenanceService>();

    public NotificationInbox Inbox => Resolve<NotificationInbox>();

    public IEventRepository EventStore => Resolve<IEventRepository>();

    public IBookingRepository BookingStore => Resolve<IBookingRepository>();

    public IUserRepository UserStore => Resolve<IUserRepository>();

    public IVenueRepository VenueStore => Resolve<IVenueRepository>();

    // --- fixtures --------------------------------------------------------------------------------

    public Venue AddVenue(int capacity = 1000, string city = "Sarajevo")
    {
        var venue = new Venue(VenueId.New(), "Dvorana", city, capacity);
        VenueStore.Add(venue);
        return venue;
    }

    public Organizer AddOrganizer()
    {
        var organizer = new Organizer(UserId.New(), "Amila", NextEmail(), "Skyline Events");
        UserStore.Add(organizer);
        return organizer;
    }

    public Customer AddCustomer(MembershipTier tier = MembershipTier.Standard, string name = "Kupac")
    {
        var customer = new Customer(UserId.New(), name, NextEmail(), tier);
        UserStore.Add(customer);
        return customer;
    }

    public ConcertEvent PublishConcert(int seats = 100, double daysAhead = 45, string city = "Sarajevo")
    {
        var concert = Catalog.CreateConcert(new CreateConcertRequest
        {
            Title = "Koncert",
            Description = "Opis koncerta",
            Schedule = Schedule(daysAhead),
            VenueId = AddVenue(city: city).Id,
            OrganizerId = AddOrganizer().Id,
            Headliner = "Headliner",
        });

        AddTicketType(concert.Id, "Parter", TicketTier.Standard, 40m, seats);
        Catalog.Publish(concert.Id);
        return concert;
    }

    public WorkshopEvent PublishWorkshop(int seats = 10, int minimumAttendees = 4, double daysAhead = 20)
    {
        var workshop = Catalog.CreateWorkshop(new CreateWorkshopRequest
        {
            Title = "Radionica",
            Description = "Opis radionice",
            Schedule = Schedule(daysAhead, hours: 6),
            VenueId = AddVenue(capacity: 50).Id,
            OrganizerId = AddOrganizer().Id,
            Instructor = "Instruktor",
            MinimumAttendees = minimumAttendees,
        });

        AddTicketType(workshop.Id, "Kotizacija", TicketTier.Standard, 200m, seats);
        Catalog.Publish(workshop.Id);
        return workshop;
    }

    public TicketType AddTicketType(
        EventId eventId,
        string name,
        TicketTier tier,
        decimal price,
        int capacity,
        DateRange? salesWindow = null) =>
        Catalog.AddTicketType(eventId, new AddTicketTypeRequest
        {
            Name = name,
            Tier = tier,
            Price = Money.Of(price),
            Capacity = capacity,
            SalesWindow = salesWindow,
        });

    public DateRange Schedule(double daysAhead, double hours = 3) =>
        DateRange.Starting(Clock.UtcNow.AddDays(daysAhead), TimeSpan.FromHours(hours));

    public static IReadOnlyCollection<TicketOrderItem> Order(Event @event, string ticketTypeName, int quantity) =>
    [
        new TicketOrderItem(
            @event.FindTicketType(ticketTypeName)?.Id
                ?? throw new InvalidOperationException($"No ticket type '{ticketTypeName}'."),
            quantity)
    ];

    public void Dispose() => _provider.Dispose();

    private EmailAddress NextEmail() => EmailAddress.Create($"korisnik{++_emailCounter}@example.ba");

    private T Resolve<T>()
        where T : notnull => _provider.GetRequiredService<T>();
}
