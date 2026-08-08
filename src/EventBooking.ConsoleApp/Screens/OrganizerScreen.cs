using EventBooking.Application.Catalog;
using EventBooking.ConsoleApp.Ui;
using EventBooking.Domain.Entities;
using EventBooking.Domain.Enums;
using EventBooking.Domain.Interfaces;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.ConsoleApp.Screens;

/// <summary>The organiser's side: create events, price them, put them on sale, call them off.</summary>
public sealed class OrganizerScreen(
    EventCatalogService catalog,
    IVenueRepository venues,
    Session session,
    IClock clock,
    IUserInterface ui) : IScreen
{
    private static readonly string[] TopLevelActions = ["Create a new event", "Manage an existing event"];

    private static readonly string[] ManageActions =
        ["Add a ticket type", "Publish", "Reschedule", "Cancel the event"];

    public string Title => "Organiser - manage my events";

    public UserRole? RequiredRole => UserRole.Organizer;

    public void Show()
    {
        var mine = catalog.GetByOrganizer(session.Organizer.Id);

        ui.Section($"{session.Organizer.OrganizationName} - {Format.Number(mine.Count)} event(s)");
        if (mine.Count > 0)
        {
            ShowTable(mine);
        }

        ui.Blank();
        switch (ui.Choose("Action", TopLevelActions, option => option))
        {
            case "Create a new event":
                CreateEvent();
                break;

            case "Manage an existing event" when mine.Count > 0:
                ManageEvent(mine);
                break;

            case "Manage an existing event":
                ui.Muted("  You have no events yet.");
                break;

            default:
                break;
        }
    }

    private void ShowTable(IReadOnlyList<Event> mine) => ui.Table(
        ["Title", "Category", "Status", "When", "Sold", "Occupancy"],
        [
            .. mine.Select(@event => new List<string>
            {
                @event.Title,
                @event.Category.ToString(),
                @event.Status.ToString(),
                Format.Moment(@event.Schedule.Start),
                Format.Ratio(@event.TicketsSold, @event.TotalCapacity),
                @event.OccupancyRate.ToString(),
            })
        ]);

    private void CreateEvent()
    {
        var category = ui.Choose(
            "What kind of event?",
            Enum.GetValues<EventCategory>().Select(value => (EventCategory?)value).ToList(),
            value => value!.Value.ToString());

        if (category is null)
        {
            return;
        }

        var title = ui.AskRequired("Title");
        var description = ui.AskRequired("Description");
        if (title is null || description is null)
        {
            return;
        }

        var venue = ui.Choose(
            "Venue",
            [.. venues.GetAll()],
            item => $"{item} (capacity {Format.Number(item.Capacity)})");

        if (venue is null)
        {
            return;
        }

        var daysAhead = ui.AskInteger("Starts in how many days?", 1, 3650);
        var hours = ui.AskInteger("Duration in hours", 1, 240);
        if (daysAhead is null || hours is null)
        {
            return;
        }

        var start = clock.UtcNow.AddDays(daysAhead.Value);
        var schedule = new DateRange(start, start.AddHours(hours.Value));

        Event? created = null;
        var succeeded = category.Value switch
        {
            EventCategory.Concert => ui.Try(() => created = CreateConcert(title, description, schedule, venue.Id)),
            EventCategory.Conference => ui.Try(() => created = CreateConference(title, description, schedule, venue.Id)),
            _ => ui.Try(() => created = CreateWorkshop(title, description, schedule, venue.Id)),
        };

        if (!succeeded || created is null)
        {
            return;
        }

        ui.Success($"Created '{created.Title}' as a draft.");
        ui.Muted("  A draft needs at least one ticket type before it can go on sale.");
        AddTicketTypes(created);
    }

    private ConcertEvent? CreateConcert(string title, string description, DateRange schedule, VenueId venueId)
    {
        var headliner = ui.AskRequired("Headliner");

        return headliner is null ? null : catalog.CreateConcert(new CreateConcertRequest
        {
            Title = title,
            Description = description,
            Schedule = schedule,
            VenueId = venueId,
            OrganizerId = session.Organizer.Id,
            Headliner = headliner,
        });
    }

    private ConferenceEvent? CreateConference(string title, string description, DateRange schedule, VenueId venueId)
    {
        var topic = ui.AskRequired("Topic");

        return topic is null ? null : catalog.CreateConference(new CreateConferenceRequest
        {
            Title = title,
            Description = description,
            Schedule = schedule,
            VenueId = venueId,
            OrganizerId = session.Organizer.Id,
            Topic = topic,
        });
    }

    private WorkshopEvent? CreateWorkshop(string title, string description, DateRange schedule, VenueId venueId)
    {
        var instructor = ui.AskRequired("Instructor");
        var minimum = ui.AskInteger("Minimum number of attendees", 1, 500);

        return instructor is null || minimum is null ? null : catalog.CreateWorkshop(new CreateWorkshopRequest
        {
            Title = title,
            Description = description,
            Schedule = schedule,
            VenueId = venueId,
            OrganizerId = session.Organizer.Id,
            Instructor = instructor,
            MinimumAttendees = minimum.Value,
        });
    }

    private void ManageEvent(IReadOnlyList<Event> mine)
    {
        var @event = ui.Choose("Which event?", mine, item => $"{item.Title} ({item.Status})");
        if (@event is null)
        {
            return;
        }

        ui.Blank();
        switch (ui.Choose("Action", ManageActions, option => option))
        {
            case "Add a ticket type":
                AddTicketTypes(@event);
                break;

            case "Publish":
                if (ui.Try(() => catalog.Publish(@event.Id, session.Organizer.Id)))
                {
                    ui.Success($"'{@event.Title}' is on sale.");
                }

                break;

            case "Reschedule":
                Reschedule(@event);
                break;

            case "Cancel the event":
                CancelEvent(@event);
                break;

            default:
                break;
        }
    }

    private void AddTicketTypes(Event @event)
    {
        while (true)
        {
            ui.Blank();
            ui.Muted($"  Remaining venue capacity: {Format.Number(@event.Venue.Capacity - @event.TotalCapacity)}");

            var name = ui.Ask("Ticket type name (Enter to stop)");
            if (name is null)
            {
                return;
            }

            var tier = ui.Choose(
                "Tier",
                Enum.GetValues<TicketTier>().Select(value => (TicketTier?)value).ToList(),
                value => value!.Value.ToString());

            var price = ui.AskDecimal("Price (BAM)");
            var capacity = ui.AskInteger("How many tickets?", 1, @event.Venue.Capacity);

            if (tier is null || price is null || capacity is null)
            {
                ui.Muted("  Skipped.");
                continue;
            }

            var added = ui.Try(() => catalog.AddTicketType(@event.Id, session.Organizer.Id, new AddTicketTypeRequest
            {
                Name = name,
                Tier = tier.Value,
                Price = Money.Of(price.Value),
                Capacity = capacity.Value,
            }));

            if (added)
            {
                ui.Success($"Added '{name}'.");
            }
        }
    }

    private void Reschedule(Event @event)
    {
        var daysAhead = ui.AskInteger("New start, in how many days from now?", 1, 3650);
        var hours = ui.AskInteger("Duration in hours", 1, 240);
        if (daysAhead is null || hours is null)
        {
            return;
        }

        var start = clock.UtcNow.AddDays(daysAhead.Value);
        var newSchedule = new DateRange(start, start.AddHours(hours.Value));

        if (ui.Try(() => catalog.Reschedule(@event.Id, session.Organizer.Id, newSchedule)))
        {
            ui.Success("Rescheduled. Everyone holding tickets has been notified.");
        }
    }

    private void CancelEvent(Event @event)
    {
        ui.Warning($"  {Format.Number(@event.TicketsSold)} ticket(s) sold; every one of them is refunded in full.");

        var reason = ui.AskRequired("Reason");
        if (reason is null || !ui.Confirm($"Really cancel '{@event.Title}'?"))
        {
            return;
        }

        var refunded = 0;
        if (ui.Try(() => refunded = catalog.Cancel(@event.Id, session.Organizer.Id, reason)))
        {
            ui.Success($"Cancelled. {Format.Number(refunded)} booking(s) were cancelled and refunded.");
        }
    }
}
