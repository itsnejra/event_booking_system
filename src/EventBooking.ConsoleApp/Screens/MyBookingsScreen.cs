using EventBooking.Application.Bookings;
using EventBooking.Application.Catalog;
using EventBooking.ConsoleApp.Ui;
using EventBooking.Domain.Abstractions;
using EventBooking.Domain.Bookings;
using EventBooking.Domain.Users;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.ConsoleApp.Screens;

/// <summary>Everything the signed-in customer has booked, and what they can still do about it.</summary>
public sealed class MyBookingsScreen(
    BookingService bookings,
    EventCatalogService catalog,
    Session session,
    IClock clock,
    IUserInterface ui) : IScreen
{
    public string Title => "My bookings";

    public UserRole? RequiredRole => UserRole.Customer;

    public void Show()
    {
        var mine = bookings.GetForCustomer(session.Customer.Id);

        if (mine.Count == 0)
        {
            ui.Muted("  No bookings yet.");
            return;
        }

        ShowTable(mine);
        ui.Blank();

        var booking = ui.Choose("Open booking", mine, item => $"{item.Reference} ({item.Status})");
        if (booking is not null)
        {
            ShowBooking(booking);
        }
    }

    private void ShowTable(IReadOnlyList<Booking> mine) => ui.Table(
        ["Reference", "Event", "Tickets", "Total", "Status", "Placed"],
        [
            .. mine.Select(booking => new List<string>
            {
                booking.Reference.Value,
                catalog.GetById(booking.EventId).Title,
                Format.Number(booking.TotalTickets),
                booking.Total.ToString(),
                DescribeStatus(booking),
                Format.Moment(booking.CreatedAt),
            })
        ]);

    private void ShowBooking(Booking booking)
    {
        var @event = catalog.GetById(booking.EventId);

        ui.Clear();
        ui.Header($"Booking {booking.Reference}");

        ui.Bullet("Event", @event.Title);
        ui.Bullet("When", Format.Range(@event.Schedule));
        ui.Bullet("Status", DescribeStatus(booking));
        ui.Bullet("Placed", Format.Moment(booking.CreatedAt));

        if (booking.ConfirmedAt is { } confirmedAt)
        {
            ui.Bullet("Paid", Format.Moment(confirmedAt));
        }

        if (booking.CancelledAt is { } cancelledAt)
        {
            ui.Bullet("Cancelled", Format.Moment(cancelledAt));
            ui.Bullet("Reason", booking.CancellationReason ?? "-");
            ui.Bullet("Refunded", booking.RefundAmount?.ToString() ?? "-");
        }

        ui.Section("Lines");
        foreach (var line in booking.Lines)
        {
            ui.Write($"  {Format.Number(line.Quantity)} x {line.TicketTypeName} @ {line.UnitPrice} = {line.Subtotal}");
            foreach (var discount in line.AppliedDiscounts)
            {
                ui.Muted($"      {discount.RuleName}: -{discount.Rate} = -{discount.Amount}");
            }
        }

        ui.Blank();
        ui.Bullet("Total", booking.Total.ToString());

        ui.Section("Refund policy of this event");
        ui.Muted($"  {@event.RefundPolicy.Description}");

        OfferActions(
            booking,
            @event.RefundPolicy.CalculateRefund(booking.Total, @event.Schedule.Start, clock.UtcNow));
    }

    private void OfferActions(Booking booking, Money estimatedRefund)
    {
        if (!booking.HoldsSeats)
        {
            return;
        }

        ui.Blank();

        if (booking.Status == BookingStatus.Pending)
        {
            ui.Warning($"  This is an unpaid hold; it lapses at {Format.Moment(booking.HoldExpiresAt)}.");

            if (ui.Confirm("Pay now?") && ui.Try(() => bookings.Confirm(booking.Id)))
            {
                ui.Success("Booking confirmed.");
                return;
            }
        }
        else
        {
            ui.Muted($"  Cancelling right now would refund {estimatedRefund}.");
        }

        if (!ui.Confirm("Cancel this booking?"))
        {
            return;
        }

        var reason = ui.Ask("Reason") ?? "Cancelled by the customer.";
        if (ui.Try(() => bookings.Cancel(booking.Id, reason)))
        {
            ui.Success($"Cancelled. Refund: {bookings.GetById(booking.Id).RefundAmount}.");
        }
    }

    private static string DescribeStatus(Booking booking) => booking.Status switch
    {
        BookingStatus.Pending => "Pending payment",
        BookingStatus.Cancelled when booking.RefundAmount is { IsPositive: true } refund => $"Cancelled (-{refund})",
        _ => booking.Status.ToString(),
    };
}
