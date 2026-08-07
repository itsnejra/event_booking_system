using EventBooking.Application.Bookings;
using EventBooking.Application.Catalog;
using EventBooking.ConsoleApp.Ui;
using EventBooking.Domain.Abstractions;
using EventBooking.Domain.Bookings;
using EventBooking.Domain.Events;
using EventBooking.Domain.Users;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.ConsoleApp.Screens;

/// <summary>
/// The customer's side: search the catalogue, open an event, book or join the waiting list.
/// </summary>
public sealed class EventCatalogScreen(
    EventCatalogService catalog,
    BookingService bookings,
    Session session,
    IClock clock,
    IUserInterface ui) : IScreen
{
    private static readonly string[] EventActions = ["Book tickets", "Join the waiting list"];

    public string Title => "Browse and book events";

    public UserRole? RequiredRole => UserRole.Customer;

    public void Show()
    {
        var criteria = AskForCriteria();
        var results = catalog.Search(criteria);

        ui.Section($"{Format.Number(results.Count)} event(s) found");
        if (results.Count == 0)
        {
            ui.Muted("  Try widening the filters, or include sold out and unpublished events.");
            return;
        }

        ShowResults(results);
        ui.Blank();

        var chosen = ui.Choose("Open event", results, @event => @event.Title);
        if (chosen is not null)
        {
            ShowEvent(chosen);
        }
    }

    private EventSearchCriteria AskForCriteria()
    {
        ui.Section("Filters - press Enter to skip any of them");

        var text = ui.Ask("Search text");

        // Nullable options, so that "skipped" and "picked the first category" stay distinguishable.
        var categories = Enum.GetValues<EventCategory>().Select(value => (EventCategory?)value).ToList();
        var category = ui.Choose("Category", categories, value => value!.Value.ToString());

        var city = ui.Ask("City");
        var maxPrice = ui.AskDecimal("Maximum price (BAM)");
        var includeSoldOut = ui.Confirm("Include sold out and unpublished events?");

        return new EventSearchCriteria
        {
            Text = text,
            Category = category,
            City = city,
            MaxPrice = maxPrice is null ? null : Money.Of(maxPrice.Value),
            OnlyBookable = !includeSoldOut,
        };
    }

    private void ShowResults(IReadOnlyList<Event> results) => ui.Table(
        ["Title", "Category", "When", "Venue", "Available", "From"],
        [
            .. results.Select(@event => new List<string>
            {
                @event.Title,
                @event.Category.ToString(),
                Format.Moment(@event.Schedule.Start),
                @event.Venue.ToString(),
                Format.Availability(@event),
                @event.CheapestTicketPrice?.ToString() ?? "-",
            })
        ]);

    private void ShowEvent(Event @event)
    {
        ui.Clear();
        ui.Header(@event.Title);

        ui.Bullet("Category", @event.Category.ToString());
        ui.Bullet("When", Format.Range(@event.Schedule));
        ui.Bullet("Venue", @event.Venue.ToString());
        ui.Bullet("Status", @event.Status.ToString());
        ui.Bullet("Occupancy", $"{@event.OccupancyRate} ({Format.Ratio(@event.TicketsSold, @event.TotalCapacity)})");
        ui.Bullet("Max per booking", Format.Number(@event.MaxTicketsPerBooking));
        ui.Bullet("Refunds", @event.RefundPolicy.Description);
        ShowTypeSpecificDetails(@event);

        ui.Blank();
        ui.Write(@event.Description);

        ui.Section("Tickets");
        ShowTicketTypes(@event);

        ui.Blank();
        switch (ui.Choose("Action", EventActions, option => option))
        {
            case "Book tickets":
                Book(@event);
                break;

            case "Join the waiting list":
                JoinWaitlist(@event);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// The one place in the console that asks what kind of event it is looking at, and it does so
    /// only to show extra detail. Everything else works against <see cref="Event"/>, because
    /// everything else genuinely is the same for all three kinds.
    /// </summary>
    private void ShowTypeSpecificDetails(Event @event)
    {
        switch (@event)
        {
            case ConcertEvent concert:
                ui.Bullet("Headliner", concert.Headliner);
                ui.Bullet("VIP limit", $"{Format.Number(ConcertEvent.MaxVipTicketsPerBooking)} per booking");
                break;

            case ConferenceEvent conference:
                ui.Bullet("Topic", conference.Topic);
                ui.Bullet("Days", Format.Number(conference.Schedule.DayCount));
                ui.Bullet("Tracks", string.Join(", ", conference.Tracks));
                ui.Blank();
                ui.Muted("  Programme:");
                foreach (var talk in conference.Sessions.OrderBy(item => item.Slot.Start))
                {
                    ui.Muted($"    {Format.Range(talk.Slot)}  [{talk.Track}] {talk.Title} - {talk.Speaker}");
                }

                break;

            case WorkshopEvent workshop:
                ui.Bullet("Instructor", workshop.Instructor);
                ui.Bullet("Minimum group", DescribeViability(workshop));
                break;

            default:
                break;
        }
    }

    private void ShowTicketTypes(Event @event) => ui.Table(
        ["Ticket", "Tier", "Price", "Available", "On sale"],
        [
            .. @event.TicketTypes.Select(type => new List<string>
            {
                type.Name,
                type.Tier.ToString(),
                type.BasePrice.ToString(),
                Format.Number(type.Allocation.Available),
                type.IsOnSale(clock.UtcNow) ? "yes" : $"no ({Format.Range(type.SalesWindow!)})",
            })
        ]);

    private void Book(Event @event)
    {
        var items = AskForTickets(@event);
        if (items.Count == 0)
        {
            ui.Muted("  Nothing selected.");
            return;
        }

        Booking? booking = null;
        if (!ui.Try(() => booking = bookings.PlaceHold(session.Customer.Id, @event.Id, items)) || booking is null)
        {
            return;
        }

        ui.Blank();
        ui.Success($"Seats held under {booking.Reference} until {Format.Moment(booking.HoldExpiresAt)}.");
        ShowPriceBreakdown(booking);

        ui.Blank();
        if (!ui.Confirm("Pay now and confirm the booking?"))
        {
            ui.Muted("  Left as a hold. The seats are released automatically when the hold lapses.");
            return;
        }

        if (ui.Try(() => bookings.Confirm(booking.Id)))
        {
            ui.Success($"Booking {booking.Reference} confirmed - the confirmation is in the notification inbox.");
        }
    }

    private List<TicketOrderItem> AskForTickets(Event @event)
    {
        ui.Blank();
        ui.Muted($"  How many of each? Up to {Format.Number(@event.MaxTicketsPerBooking)} in total, Enter for none.");

        var items = new List<TicketOrderItem>();
        foreach (var ticketType in @event.TicketTypes)
        {
            var quantity = ui.AskInteger(
                $"  {ticketType.Name} ({ticketType.BasePrice})",
                0,
                @event.MaxTicketsPerBooking);

            if (quantity is > 0)
            {
                items.Add(new TicketOrderItem(ticketType.Id, quantity.Value));
            }
        }

        return items;
    }

    private void ShowPriceBreakdown(Booking booking)
    {
        ui.Section("Price breakdown");

        foreach (var line in booking.Lines)
        {
            ui.Write($"  {Format.Number(line.Quantity)} x {line.TicketTypeName} @ {line.UnitPrice} = {line.Subtotal}");
            foreach (var discount in line.AppliedDiscounts)
            {
                ui.Muted($"      {discount.RuleName}: -{discount.Rate} = -{discount.Amount}");
            }
        }

        ui.Blank();
        ui.Bullet("Subtotal", booking.Subtotal.ToString());
        ui.Bullet("Discounts", $"-{booking.DiscountTotal}");
        ui.Bullet("Total", booking.Total.ToString());
    }

    private void JoinWaitlist(Event @event)
    {
        var quantity = ui.AskInteger("How many tickets do you want?", 1, @event.MaxTicketsPerBooking);
        if (quantity is null)
        {
            return;
        }

        if (ui.Try(() => bookings.JoinWaitlist(session.Customer.Id, @event.Id, quantity.Value)))
        {
            ui.Success("You are on the waiting list. You will be notified as soon as seats are released.");
        }
    }

    private static string DescribeViability(WorkshopEvent workshop) => workshop.IsViable
        ? $"{Format.Number(workshop.MinimumAttendees)} (reached)"
        : $"{Format.Number(workshop.MinimumAttendees)} ({Format.Number(workshop.SeatsUntilViable)} more needed)";
}
