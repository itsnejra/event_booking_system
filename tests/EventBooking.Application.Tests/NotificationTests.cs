using EventBooking.Application.Tests.TestKit;
using EventBooking.Domain.Bookings;

namespace EventBooking.Application.Tests;

/// <summary>
/// These are the tests that prove the domain events are worth having: no service in the flow knows
/// that notifications exist, yet the right person is told at the right moment.
/// </summary>
public sealed class NotificationTests
{
    [Fact]
    public void ConfirmingABooking_SendsAConfirmation()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert();
        var customer = host.AddCustomer();
        var booking = host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 1));

        host.Bookings.Confirm(booking.Id);

        var message = Assert.Single(host.Inbox.For(customer.Email));
        Assert.Contains(booking.Reference.Value, message.Notification.Subject, StringComparison.Ordinal);
    }

    [Fact]
    public void HoldingSeatsAlone_TellsNobodyAnything()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert();
        var customer = host.AddCustomer();

        host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 1));

        Assert.Empty(host.Inbox.For(customer.Email));
    }

    [Fact]
    public void CancellingABooking_ExplainsTheRefund()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(daysAhead: 45);
        var customer = host.AddCustomer();
        var booking = host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 1));
        host.Bookings.Confirm(booking.Id);

        host.Bookings.Cancel(booking.Id, "Promjena planova");

        var delivered = host.Inbox.For(customer.Email);
        var message = delivered[^1];
        Assert.Contains("cancelled", message.Notification.Subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(booking.RefundAmount!.Value.ToString(), message.Notification.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void AnExpiredHold_TellsTheCustomerTheSeatsAreGone()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert();
        var customer = host.AddCustomer();
        host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 1));

        host.Clock.Advance(Booking.DefaultHoldDuration);
        host.Bookings.ExpireStaleHolds();

        var message = Assert.Single(host.Inbox.For(customer.Email));
        Assert.Contains("expired", message.Notification.Subject, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReschedulingAnEvent_NotifiesEveryoneStillHoldingTickets()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 20, daysAhead: 45);
        var attending = host.AddCustomer(name: "Ide");
        var cancelled = host.AddCustomer(name: "Otkazao");

        var keep = host.Bookings.PlaceHold(attending.Id, concert.Id, TestHost.Order(concert, "Parter", 1));
        host.Bookings.Confirm(keep.Id);

        var drop = host.Bookings.PlaceHold(cancelled.Id, concert.Id, TestHost.Order(concert, "Parter", 1));
        host.Bookings.Confirm(drop.Id);
        host.Bookings.Cancel(drop.Id, "Ne mogu");

        host.Inbox.Clear();
        host.Catalog.Reschedule(concert.Id, host.Schedule(daysAhead: 90));

        Assert.Single(host.Inbox.For(attending.Email));
        Assert.Empty(host.Inbox.For(cancelled.Email));
    }

    /// <summary>
    /// Releasing seats raises a domain event; the waiting list handler picks it up. Nothing in
    /// <c>BookingService</c> mentions the waiting list at all.
    /// </summary>
    [Fact]
    public void ReleasedSeats_ReachTheFrontOfTheWaitingList()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 2, daysAhead: 45);
        var buyer = host.AddCustomer(name: "Kupac");
        var waiting = host.AddCustomer(name: "Ceka");

        var booking = host.Bookings.PlaceHold(buyer.Id, concert.Id, TestHost.Order(concert, "Parter", 2));
        host.Bookings.Confirm(booking.Id);
        host.Bookings.JoinWaitlist(waiting.Id, concert.Id, 2);
        host.Inbox.Clear();

        host.Bookings.Cancel(booking.Id, "Promjena planova");

        var message = Assert.Single(host.Inbox.For(waiting.Email));
        Assert.Contains("available", message.Notification.Subject, StringComparison.OrdinalIgnoreCase);
        Assert.False(concert.Waitlist[0].IsWaiting);
    }

    [Fact]
    public void CancellingAnEvent_NotifiesEveryAffectedCustomer()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 20, daysAhead: 45);
        var first = host.AddCustomer(name: "Prvi");
        var second = host.AddCustomer(name: "Drugi");

        foreach (var customer in new[] { first, second })
        {
            var booking = host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 1));
            host.Bookings.Confirm(booking.Id);
        }

        host.Inbox.Clear();
        host.Catalog.Cancel(concert.Id, "Bolest izvodjaca");

        Assert.Single(host.Inbox.For(first.Email));
        Assert.Single(host.Inbox.For(second.Email));
    }
}
