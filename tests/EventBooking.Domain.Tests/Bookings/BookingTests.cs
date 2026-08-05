using EventBooking.Domain.Bookings;
using EventBooking.Domain.Events;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.Pricing;
using EventBooking.Domain.Pricing.Rules;
using EventBooking.Domain.Refunds;
using EventBooking.Domain.Tests.TestKit;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Tests.Bookings;

public sealed class BookingTests
{
    [Fact]
    public void PlaceHold_StartsPendingWithAHoldWindow()
    {
        var booking = APendingBooking(out _);

        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(Given.Now + Booking.DefaultHoldDuration, booking.HoldExpiresAt);
        Assert.True(booking.HoldsSeats);
        Assert.False(booking.IsPaidFor);
    }

    [Fact]
    public void PlaceHold_RaisesBookingPlaced()
    {
        var booking = APendingBooking(out _);

        Assert.Single(booking.DomainEvents.OfType<BookingPlacedDomainEvent>());
    }

    [Fact]
    public void Totals_AreTheSumOfTheLines()
    {
        var booking = APendingBooking(out _, quantity: 2, daysAhead: 45);

        Assert.Equal(Money.Of(80m), booking.Subtotal);
        Assert.Equal(Money.Of(12m), booking.DiscountTotal);
        Assert.Equal(Money.Of(68m), booking.Total);
        Assert.Equal(2, booking.TotalTickets);
    }

    [Fact]
    public void Confirm_MarksItPaidAndRaisesBookingConfirmed()
    {
        var booking = APendingBooking(out _);
        booking.ClearDomainEvents();

        booking.Confirm(Given.Now.AddMinutes(1));

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.True(booking.IsPaidFor);
        Assert.Equal(Given.Now.AddMinutes(1), booking.ConfirmedAt);
        Assert.Single(booking.DomainEvents.OfType<BookingConfirmedDomainEvent>());
    }

    [Fact]
    public void Confirm_AfterTheHoldHasLapsed_Throws()
    {
        var booking = APendingBooking(out _);

        Assert.Throws<BusinessRuleViolationException>(() => booking.Confirm(booking.HoldExpiresAt));
    }

    [Fact]
    public void Confirm_Twice_Throws()
    {
        var booking = APendingBooking(out _);
        booking.Confirm(Given.Now);

        Assert.Throws<InvalidStateTransitionException>(() => booking.Confirm(Given.Now));
    }

    [Fact]
    public void Expire_OnlyFiresOnceTheHoldWindowHasPassed()
    {
        var booking = APendingBooking(out _);

        Assert.False(booking.Expire(Given.Now.AddMinutes(1)));
        Assert.Equal(BookingStatus.Pending, booking.Status);

        Assert.True(booking.Expire(booking.HoldExpiresAt));
        Assert.Equal(BookingStatus.Expired, booking.Status);
    }

    [Fact]
    public void Expire_IsIdempotent()
    {
        var booking = APendingBooking(out _);
        booking.Expire(booking.HoldExpiresAt);
        booking.ClearDomainEvents();

        Assert.False(booking.Expire(booking.HoldExpiresAt.AddHours(1)));
        Assert.Empty(booking.DomainEvents);
    }

    [Fact]
    public void Expire_DoesNotTouchAConfirmedBooking()
    {
        var booking = APendingBooking(out _);
        booking.Confirm(Given.Now);

        Assert.False(booking.Expire(Given.Now.AddDays(1)));
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void Cancel_OfAPaidBooking_RefundsAccordingToThePolicy()
    {
        var booking = APendingBooking(out var concert, daysAhead: 45);
        booking.Confirm(Given.Now);

        var refund = booking.Cancel(concert.RefundPolicy, concert.Schedule.Start, Given.Now, "Promjena planova");

        Assert.Equal(booking.Total, refund);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(refund, booking.RefundAmount);
        Assert.Equal("Promjena planova", booking.CancellationReason);
    }

    [Fact]
    public void Cancel_CloseToTheEvent_RefundsLess()
    {
        var booking = APendingBooking(out var concert, daysAhead: 45);
        booking.Confirm(Given.Now);
        var fiveDaysBefore = concert.Schedule.Start.AddDays(-5);

        var refund = booking.Cancel(concert.RefundPolicy, concert.Schedule.Start, fiveDaysBefore, "Kasno");

        Assert.Equal(booking.Total.Portion(Percentage.Of(50m)), refund);
    }

    [Fact]
    public void Cancel_OfAnUnpaidHold_RefundsNothing()
    {
        var booking = APendingBooking(out var concert);

        var refund = booking.Cancel(concert.RefundPolicy, concert.Schedule.Start, Given.Now, "Predomislio se");

        Assert.True(refund.IsZero);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public void Cancel_RaisesBookingCancelledWithWhetherItHadBeenPaidFor()
    {
        var booking = APendingBooking(out var concert);
        booking.Confirm(Given.Now);
        booking.ClearDomainEvents();

        booking.Cancel(concert.RefundPolicy, concert.Schedule.Start, Given.Now, "Razlog");

        var raised = Assert.Single(booking.DomainEvents.OfType<BookingCancelledDomainEvent>());
        Assert.True(raised.WasPaidFor);
    }

    [Fact]
    public void Cancel_Twice_Throws()
    {
        var booking = APendingBooking(out var concert);
        booking.Cancel(concert.RefundPolicy, concert.Schedule.Start, Given.Now, "Razlog");

        Assert.Throws<InvalidStateTransitionException>(
            () => booking.Cancel(concert.RefundPolicy, concert.Schedule.Start, Given.Now, "Opet"));
    }

    /// <summary>
    /// When the organiser calls the event off, the event's own refund rule must not apply - the
    /// customer carries none of the risk for somebody else's decision.
    /// </summary>
    [Fact]
    public void CancelBecauseEventCancelled_RefundsEverythingEvenAtTheLastMinute()
    {
        var booking = APendingBooking(out var concert, daysAhead: 45);
        booking.Confirm(Given.Now);
        var anHourBefore = concert.Schedule.Start.AddHours(-1);

        var refund = booking.CancelBecauseEventCancelled(anHourBefore, "Koncert otkazan");

        Assert.Equal(booking.Total, refund);
        Assert.True(booking.RefundAmount is { IsPositive: true });
    }

    [Fact]
    public void AsReservation_RebuildsExactlyWhatWasBooked()
    {
        var booking = APendingBooking(out _, quantity: 3);

        var reservation = booking.AsReservation();

        Assert.Equal(booking.EventId, reservation.EventId);
        Assert.Equal(3, reservation.TotalQuantity);
        Assert.Equal(booking.Lines[0].TicketTypeId, reservation.Lines[0].TicketTypeId);
        Assert.Equal(booking.Lines[0].UnitPrice, reservation.Lines[0].UnitPrice);
    }

    [Fact]
    public void ALineKeepsTheReasonsForItsDiscounts()
    {
        var booking = APendingBooking(out _, daysAhead: 45);

        var line = Assert.Single(booking.Lines);
        Assert.Equal(EarlyBirdDiscountRule.Default.Name, Assert.Single(line.AppliedDiscounts).RuleName);
        Assert.Equal(line.Discount, line.AppliedDiscounts[0].Amount);
    }

    private static Booking APendingBooking(out ConcertEvent concert, int quantity = 1, double daysAhead = 5)
    {
        concert = Given.PublishedConcert(daysAhead: daysAhead);

        var reservation = concert.Reserve(Given.Order(concert, "Parter", quantity), Given.Now);
        var priced = new PricingEngine([EarlyBirdDiscountRule.Default]).Price(
            concert,
            Given.Customer(),
            reservation,
            Given.Now);

        return Booking.PlaceHold(
            BookingId.New(),
            BookingReference.For(Given.Now, 1),
            UserId.New(),
            priced,
            Given.Now);
    }
}
