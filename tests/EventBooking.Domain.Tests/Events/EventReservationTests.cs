using EventBooking.Domain.Events;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.Tests.TestKit;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Tests.Events;

public sealed class EventReservationTests
{
    [Fact]
    public void Reserve_HoldsSeatsWithoutSellingThem()
    {
        var concert = Given.PublishedConcert(standardSeats: 50);

        concert.Reserve(Given.Order(concert, "Parter", 3), Given.Now);

        Assert.Equal(3, concert.TicketsOnHold);
        Assert.Equal(0, concert.TicketsSold);
        Assert.Equal(57, concert.AvailableTickets);
    }

    [Fact]
    public void Reserve_CapturesTheListPriceAtTheTimeOfBooking()
    {
        var concert = Given.PublishedConcert();

        var reservation = concert.Reserve(Given.Order(concert, "Parter", 2), Given.Now);
        concert.GetTicketType(Given.TicketTypeOf(concert, "Parter")).ChangePrice(Money.Of(999m));

        Assert.Equal(Money.Of(40m), reservation.Lines[0].UnitPrice);
    }

    [Fact]
    public void Reserve_OnADraftEvent_Throws()
    {
        var concert = Given.Concert();
        concert.AddTicketType(Given.OrganizerId, "Parter", TicketTier.Standard, Money.Of(40m), 10);

        Assert.Throws<InvalidStateTransitionException>(
            () => concert.Reserve(Given.Order(concert, "Parter", 1), Given.Now));
    }

    [Fact]
    public void Reserve_OnACancelledEvent_Throws()
    {
        var concert = Given.PublishedConcert();
        concert.Cancel(Given.OrganizerId, "Otkazano", Given.Now);

        Assert.Throws<InvalidStateTransitionException>(
            () => concert.Reserve(Given.Order(concert, "Parter", 1), Given.Now));
    }

    [Fact]
    public void Reserve_AfterTheEventHasStarted_Throws()
    {
        var concert = Given.PublishedConcert();

        Assert.Throws<BusinessRuleViolationException>(
            () => concert.Reserve(Given.Order(concert, "Parter", 1), Given.Now.AddDays(46)));
    }

    [Fact]
    public void Reserve_WithNothingInTheOrder_Throws()
    {
        var concert = Given.PublishedConcert();

        Assert.Throws<BusinessRuleViolationException>(() => concert.Reserve([], Given.Now));
    }

    [Fact]
    public void Reserve_MoreThanIsLeft_ThrowsAndReportsTheNumbers()
    {
        var concert = Given.PublishedConcert(standardSeats: 2, vipSeats: 0);

        var exception = Assert.Throws<InsufficientTicketsException>(
            () => concert.Reserve(Given.Order(concert, "Parter", 5), Given.Now));

        Assert.Equal(5, exception.Requested);
        Assert.Equal(2, exception.Available);
    }

    [Fact]
    public void Reserve_MergesDuplicateLinesBeforeCheckingAvailability()
    {
        var concert = Given.PublishedConcert(standardSeats: 3, vipSeats: 0);
        var parter = Given.TicketTypeOf(concert, "Parter");

        Assert.Throws<InsufficientTicketsException>(() => concert.Reserve(
            [new TicketOrderItem(parter, 2), new TicketOrderItem(parter, 2)],
            Given.Now));
    }

    /// <summary>
    /// The point of validating the whole order before taking a single seat: a request that fails
    /// must leave the inventory exactly as it found it.
    /// </summary>
    [Fact]
    public void Reserve_WhenALaterLineFails_LeavesNoSeatsHeld()
    {
        var concert = Given.PublishedConcert(standardSeats: 50, vipSeats: 1);

        Assert.Throws<InsufficientTicketsException>(() => concert.Reserve(
            [
                new TicketOrderItem(Given.TicketTypeOf(concert, "Parter"), 2),
                new TicketOrderItem(Given.TicketTypeOf(concert, "VIP"), 2),
            ],
            Given.Now));

        Assert.Equal(0, concert.TicketsOnHold);
        Assert.Equal(51, concert.AvailableTickets);
    }

    [Fact]
    public void Reserve_OutsideTheSalesWindow_Throws()
    {
        var concert = Given.Concert(daysAhead: 40);
        var window = new DateRange(Given.Now.AddDays(1), Given.Now.AddDays(5));
        concert.AddTicketType(Given.OrganizerId, "Prvo kolo", TicketTier.Standard, Money.Of(30m), 100, window);
        concert.AddTicketType(Given.OrganizerId, "Parter", TicketTier.Standard, Money.Of(40m), 100);
        concert.Publish(Given.OrganizerId, Given.Now);

        Assert.Throws<BusinessRuleViolationException>(
            () => concert.Reserve(Given.Order(concert, "Prvo kolo", 1), Given.Now.AddDays(10)));
    }

    [Fact]
    public void Reserve_InsideTheSalesWindow_Succeeds()
    {
        var concert = Given.Concert(daysAhead: 40);
        var window = new DateRange(Given.Now.AddDays(1), Given.Now.AddDays(5));
        concert.AddTicketType(Given.OrganizerId, "Prvo kolo", TicketTier.Standard, Money.Of(30m), 100, window);
        concert.AddTicketType(Given.OrganizerId, "Parter", TicketTier.Standard, Money.Of(40m), 100);
        concert.Publish(Given.OrganizerId, Given.Now);

        var reservation = concert.Reserve(Given.Order(concert, "Prvo kolo", 1), Given.Now.AddDays(2));

        Assert.Equal(1, reservation.TotalQuantity);
    }

    [Fact]
    public void ConfirmReservation_TurnsHeldSeatsIntoSoldSeats()
    {
        var concert = Given.PublishedConcert(standardSeats: 50);
        var reservation = concert.Reserve(Given.Order(concert, "Parter", 4), Given.Now);

        concert.ConfirmReservation(reservation);

        Assert.Equal(0, concert.TicketsOnHold);
        Assert.Equal(4, concert.TicketsSold);
    }

    [Fact]
    public void ConfirmReservation_FromAnotherEvent_Throws()
    {
        var first = Given.PublishedConcert();
        var second = Given.PublishedConcert();
        var reservation = second.Reserve(Given.Order(second, "Parter", 1), Given.Now);

        Assert.Throws<ArgumentException>(() => first.ConfirmReservation(reservation));
    }

    [Fact]
    public void ReleaseReservation_OfAnUnpaidHold_ReturnsTheSeats()
    {
        var concert = Given.PublishedConcert(standardSeats: 10, vipSeats: 0);
        var reservation = concert.Reserve(Given.Order(concert, "Parter", 4), Given.Now);

        concert.ReleaseReservation(reservation, wasPaidFor: false, Given.Now);

        Assert.Equal(10, concert.AvailableTickets);
    }

    [Fact]
    public void ReleaseReservation_OfAPaidBooking_ReturnsTheSeatsAndAnnouncesIt()
    {
        var concert = Given.PublishedConcert(standardSeats: 10, vipSeats: 0);
        var reservation = concert.Reserve(Given.Order(concert, "Parter", 4), Given.Now);
        concert.ConfirmReservation(reservation);
        concert.ClearDomainEvents();

        concert.ReleaseReservation(reservation, wasPaidFor: true, Given.Now);

        Assert.Equal(10, concert.AvailableTickets);
        Assert.Equal(0, concert.TicketsSold);

        var released = Assert.Single(concert.DomainEvents.OfType<TicketsReleasedDomainEvent>());
        Assert.Equal(4, released.Quantity);
    }

    [Fact]
    public void IsSoldOut_IsTrueOnlyWhenNothingIsLeft()
    {
        var concert = Given.PublishedConcert(standardSeats: 2, vipSeats: 0);
        Assert.False(concert.IsSoldOut);

        concert.Reserve(Given.Order(concert, "Parter", 2), Given.Now);

        Assert.True(concert.IsSoldOut);
        Assert.False(concert.IsBookable);
    }
}
