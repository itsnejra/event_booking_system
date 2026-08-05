using EventBooking.Application.Abstractions;
using EventBooking.Application.Bookings;
using EventBooking.Domain.Abstractions;
using EventBooking.Domain.Events;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.Users;
using EventBooking.Domain.ValueObjects;
using EventBooking.Domain.Venues;

namespace EventBooking.Application.Catalog;

/// <summary>
/// The organiser's side of the system: creating events, putting them on sale, and taking them off.
/// </summary>
public sealed class EventCatalogService(
    IEventRepository events,
    IVenueRepository venues,
    IUserRepository users,
    BookingService bookings,
    IDomainEventDispatcher dispatcher,
    IClock clock)
{
    public ConcertEvent CreateConcert(CreateConcertRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (venue, organizer) = Resolve(request);
        var concert = new ConcertEvent(
            EventId.New(),
            request.Title,
            request.Description,
            request.Schedule,
            venue,
            organizer.Id,
            request.Headliner,
            request.Currency);

        events.Add(concert);
        return concert;
    }

    public ConferenceEvent CreateConference(CreateConferenceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (venue, organizer) = Resolve(request);
        var conference = new ConferenceEvent(
            EventId.New(),
            request.Title,
            request.Description,
            request.Schedule,
            venue,
            organizer.Id,
            request.Topic,
            request.Currency);

        events.Add(conference);
        return conference;
    }

    public WorkshopEvent CreateWorkshop(CreateWorkshopRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (venue, organizer) = Resolve(request);
        var workshop = new WorkshopEvent(
            EventId.New(),
            request.Title,
            request.Description,
            request.Schedule,
            venue,
            organizer.Id,
            request.Instructor,
            request.MinimumAttendees,
            request.Currency);

        events.Add(workshop);
        return workshop;
    }

    public TicketType AddTicketType(EventId eventId, AddTicketTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return GetById(eventId).AddTicketType(
            request.Name,
            request.Tier,
            request.Price,
            request.Capacity,
            request.SalesWindow);
    }

    public void Publish(EventId eventId)
    {
        var @event = GetById(eventId);
        @event.Publish(clock.UtcNow);
        dispatcher.Dispatch(@event);
    }

    public void Reschedule(EventId eventId, DateRange newSchedule)
    {
        var @event = GetById(eventId);
        @event.Reschedule(newSchedule, clock.UtcNow);
        dispatcher.Dispatch(@event);
    }

    /// <summary>
    /// Calls the event off and unwinds everything that depended on it. The order matters: the event
    /// is cancelled first so that nothing new can be booked while the outstanding bookings are being
    /// refunded.
    /// </summary>
    public int Cancel(EventId eventId, string reason)
    {
        var @event = GetById(eventId);
        @event.Cancel(reason, clock.UtcNow);
        dispatcher.Dispatch(@event);

        return bookings.CancelAllForEvent(@event, $"'{@event.Title}' was cancelled: {reason}").Count;
    }

    public IReadOnlyList<Event> Search(EventSearchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var matches = events.Find(criteria.ToSpecification());
        return [.. criteria.Sort(matches)];
    }

    public IReadOnlyList<Event> GetByOrganizer(UserId organizerId) =>
        [.. events.GetByOrganizer(organizerId).OrderBy(item => item.Schedule.Start)];

    public Event GetById(EventId eventId) =>
        events.FindById(eventId) ?? throw EntityNotFoundException.For<Event>(eventId);

    private (Venue Venue, Organizer Organizer) Resolve(CreateEventRequest request)
    {
        var venue = venues.FindById(request.VenueId)
            ?? throw EntityNotFoundException.For<Venue>(request.VenueId);

        // Only an organiser may create events - the check belongs here rather than in the aggregate,
        // because it is about who is calling, not about whether the event itself is valid.
        var organizer = users.FindById(request.OrganizerId) as Organizer
            ?? throw EntityNotFoundException.For<Organizer>(request.OrganizerId);

        return (venue, organizer);
    }
}
