using EventBooking.Application.Tests.TestKit;
using EventBooking.Domain.Bookings;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.Users;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Tests;

public sealed class BookingServiceTests
{
    [Fact]
    public void PlaceHold_TakesSeatsOutOfThePoolWithoutSellingThem()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 50);
        var customer = host.AddCustomer();

        var booking = host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 3));

        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(3, concert.TicketsOnHold);
        Assert.Equal(0, concert.TicketsSold);
        Assert.Equal(47, concert.AvailableTickets);
    }

    [Fact]
    public void PlaceHold_PricesTheOrderThroughTheRuleSet()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 50, daysAhead: 45);
        var customer = host.AddCustomer(MembershipTier.Gold);

        var booking = host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 5));

        // 5 x 40.00 = 200.00, less 15% early bird + 10% group + 10% loyalty.
        Assert.Equal(Money.Of(200m), booking.Subtotal);
        Assert.Equal(Money.Of(130m), booking.Total);
        Assert.Equal(3, booking.Lines[0].AppliedDiscounts.Count);
    }

    [Fact]
    public void PlaceHold_GivesEachBookingItsOwnReference()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert();
        var customer = host.AddCustomer();

        var first = host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 1));
        var second = host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 1));

        Assert.NotEqual(first.Reference, second.Reference);
        Assert.Equal(first, host.Bookings.GetByReference(first.Reference.Value));
    }

    [Fact]
    public void PlaceHold_ForSomebodyWhoIsNotACustomer_Throws()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert();
        var organizer = host.AddOrganizer();

        Assert.Throws<EntityNotFoundException>(
            () => host.Bookings.PlaceHold(organizer.Id, concert.Id, TestHost.Order(concert, "Parter", 1)));
    }

    [Fact]
    public void PlaceHold_WhenTheOrderIsRefused_LeavesNoSeatsStranded()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 2);
        var customer = host.AddCustomer();

        Assert.Throws<InsufficientTicketsException>(
            () => host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 5)));

        Assert.Equal(2, concert.AvailableTickets);
        Assert.Empty(host.BookingStore.GetAll());
    }

    [Fact]
    public void Confirm_TurnsHeldSeatsIntoSoldOnes()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 50);
        var customer = host.AddCustomer();
        var booking = host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 2));

        host.Bookings.Confirm(booking.Id);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(2, concert.TicketsSold);
        Assert.Equal(0, concert.TicketsOnHold);
    }

    [Fact]
    public void Confirm_CountsTowardsTheCustomersLoyalty()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert();
        var customer = host.AddCustomer();

        for (var index = 0; index < Customer.SilverThreshold; index++)
        {
            var booking = host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 1));
            host.Bookings.Confirm(booking.Id);
        }

        Assert.Equal(MembershipTier.Silver, customer.Tier);
    }

    [Fact]
    public void Confirm_OnceTheHoldHasLapsed_Throws()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert();
        var customer = host.AddCustomer();
        var booking = host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 1));

        host.Clock.Advance(Booking.DefaultHoldDuration + TimeSpan.FromMinutes(1));

        Assert.Throws<BusinessRuleViolationException>(() => host.Bookings.Confirm(booking.Id));
    }

    [Fact]
    public void Confirm_ForAnEventThatHasBeenCancelled_Throws()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert();
        var customer = host.AddCustomer();
        var booking = host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 1));

        host.Catalog.Cancel(concert.Id, concert.OrganizerId, "Otkazano");

        // The service checks the event before the booking, so the customer is told why rather than
        // just that their booking is in the wrong state.
        var exception = Assert.Throws<BusinessRuleViolationException>(() => host.Bookings.Confirm(booking.Id));
        Assert.Contains("cancelled", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cancel_OfAPaidBooking_RefundsAndReturnsTheSeats()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 10, daysAhead: 45);
        var customer = host.AddCustomer();
        var booking = host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 2));
        host.Bookings.Confirm(booking.Id);

        host.Bookings.Cancel(booking.Id, "Promjena planova");

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(booking.Total, booking.RefundAmount);
        Assert.Equal(0, concert.TicketsSold);
        Assert.Equal(10, concert.AvailableTickets);
    }

    [Fact]
    public void Cancel_ShortlyBeforeTheEvent_RefundsLessButStillFreesTheSeats()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 10, daysAhead: 45);
        var customer = host.AddCustomer();
        var booking = host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 2));
        host.Bookings.Confirm(booking.Id);

        host.Clock.MoveTo(concert.Schedule.Start.AddDays(-5));
        host.Bookings.Cancel(booking.Id, "Kasna otkazivanja");

        Assert.Equal(booking.Total.Portion(Percentage.Of(50m)), booking.RefundAmount);
        Assert.Equal(10, concert.AvailableTickets);
    }

    [Fact]
    public void Cancel_OfAnUnpaidHold_FreesTheSeatsWithoutARefund()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 10);
        var customer = host.AddCustomer();
        var booking = host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 4));

        host.Bookings.Cancel(booking.Id, "Predomislio se");

        Assert.True(booking.RefundAmount is { IsZero: true });
        Assert.Equal(10, concert.AvailableTickets);
    }

    [Fact]
    public void ExpireStaleHolds_ReleasesSeatsOnlyOnceTheWindowHasPassed()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 10);
        var customer = host.AddCustomer();
        host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 3));

        Assert.Equal(0, host.Bookings.ExpireStaleHolds());
        Assert.Equal(7, concert.AvailableTickets);

        host.Clock.Advance(Booking.DefaultHoldDuration);

        Assert.Equal(1, host.Bookings.ExpireStaleHolds());
        Assert.Equal(10, concert.AvailableTickets);
    }

    [Fact]
    public void ExpireStaleHolds_LeavesPaidBookingsAlone()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 10);
        var customer = host.AddCustomer();
        var booking = host.Bookings.PlaceHold(customer.Id, concert.Id, TestHost.Order(concert, "Parter", 3));
        host.Bookings.Confirm(booking.Id);

        host.Clock.Advance(TimeSpan.FromDays(1));

        Assert.Equal(0, host.Bookings.ExpireStaleHolds());
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void GetForCustomer_ReturnsOnlyTheirBookings()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert();
        var mine = host.AddCustomer(name: "Moje");
        var theirs = host.AddCustomer(name: "Tudje");

        host.Bookings.PlaceHold(mine.Id, concert.Id, TestHost.Order(concert, "Parter", 1));
        host.Bookings.PlaceHold(theirs.Id, concert.Id, TestHost.Order(concert, "Parter", 1));

        Assert.Single(host.Bookings.GetForCustomer(mine.Id));
    }

    [Fact]
    public void JoinWaitlist_IsOnlyPossibleOnceTheEventIsFull()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 1);
        var buyer = host.AddCustomer(name: "Kupac");
        var latecomer = host.AddCustomer(name: "Zakasnio");

        Assert.Throws<BusinessRuleViolationException>(
            () => host.Bookings.JoinWaitlist(latecomer.Id, concert.Id, 1));

        var booking = host.Bookings.PlaceHold(buyer.Id, concert.Id, TestHost.Order(concert, "Parter", 1));
        host.Bookings.Confirm(booking.Id);

        var entry = host.Bookings.JoinWaitlist(latecomer.Id, concert.Id, 1);

        Assert.Equal(latecomer.Id, entry.CustomerId);
    }
}
