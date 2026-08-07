using EventBooking.Domain.Events;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.Tests.TestKit;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Tests.Events;

public sealed class EventLifecycleTests
{
    [Fact]
    public void NewEvent_StartsAsADraft()
    {
        Assert.Equal(EventStatus.Draft, Given.Concert().Status);
    }

    [Fact]
    public void Publish_WithoutTicketTypes_Throws()
    {
        var concert = Given.Concert();

        Assert.Throws<BusinessRuleViolationException>(() => concert.Publish(Given.OrganizerId, Given.Now));
    }

    [Fact]
    public void Publish_WhenTheEventHasAlreadyStarted_Throws()
    {
        var concert = Given.Concert(daysAhead: 1);
        concert.AddTicketType(Given.OrganizerId, "Parter", TicketTier.Standard, Money.Of(30m), 10);

        Assert.Throws<BusinessRuleViolationException>(() => concert.Publish(Given.OrganizerId, Given.Now.AddDays(2)));
    }

    [Fact]
    public void Publish_RaisesEventPublished()
    {
        var concert = Given.PublishedConcert();

        Assert.Contains(concert.DomainEvents, domainEvent => domainEvent is EventPublishedDomainEvent);
    }

    [Fact]
    public void Publish_Twice_Throws()
    {
        var concert = Given.PublishedConcert();

        Assert.Throws<InvalidStateTransitionException>(() => concert.Publish(Given.OrganizerId, Given.Now));
    }

    [Fact]
    public void AddTicketType_BeyondVenueCapacity_Throws()
    {
        var concert = Given.Concert(venue: Given.Venue(100));
        concert.AddTicketType(Given.OrganizerId, "Parter", TicketTier.Standard, Money.Of(30m), 80);

        var exception = Assert.Throws<BusinessRuleViolationException>(
            () => concert.AddTicketType(Given.OrganizerId, "Tribina", TicketTier.Standard, Money.Of(20m), 30));

        Assert.Contains("100", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddTicketType_WithADuplicateName_Throws()
    {
        var concert = Given.Concert();
        concert.AddTicketType(Given.OrganizerId, "Parter", TicketTier.Standard, Money.Of(30m), 50);

        Assert.Throws<BusinessRuleViolationException>(
            () => concert.AddTicketType(Given.OrganizerId, "parter", TicketTier.Vip, Money.Of(90m), 10));
    }

    [Fact]
    public void AddTicketType_InADifferentCurrency_Throws()
    {
        var concert = Given.Concert();

        Assert.Throws<CurrencyMismatchException>(
            () => concert.AddTicketType(Given.OrganizerId, "Parter", TicketTier.Standard, Money.Of(30m, Currency.EUR), 50));
    }

    [Fact]
    public void AddTicketType_WithSalesClosingAfterTheEventStarts_Throws()
    {
        var concert = Given.Concert(daysAhead: 10);
        var tooLate = DateRange.Starting(Given.Now, TimeSpan.FromDays(30));

        Assert.Throws<BusinessRuleViolationException>(
            () => concert.AddTicketType(Given.OrganizerId, "Parter", TicketTier.Standard, Money.Of(30m), 50, tooLate));
    }

    [Fact]
    public void AddTicketType_ToACancelledEvent_Throws()
    {
        var concert = Given.PublishedConcert();
        concert.Cancel(Given.OrganizerId, "Otkazano", Given.Now);

        Assert.Throws<InvalidStateTransitionException>(
            () => concert.AddTicketType(Given.OrganizerId, "Novo", TicketTier.Standard, Money.Of(10m), 5));
    }

    [Fact]
    public void Cancel_RecordsTheReasonAndRaisesEventCancelled()
    {
        var concert = Given.PublishedConcert();
        concert.ClearDomainEvents();

        concert.Cancel(Given.OrganizerId, "Bolest izvodjaca", Given.Now);

        Assert.Equal(EventStatus.Cancelled, concert.Status);
        Assert.Equal("Bolest izvodjaca", concert.CancellationReason);
        Assert.Contains(concert.DomainEvents, domainEvent => domainEvent is EventCancelledDomainEvent);
    }

    [Fact]
    public void Reschedule_ToAPastDate_Throws()
    {
        var concert = Given.PublishedConcert();

        Assert.Throws<BusinessRuleViolationException>(
            () => concert.Reschedule(Given.OrganizerId, Given.Schedule(-1), Given.Now));
    }

    [Fact]
    public void Reschedule_OfAPublishedEvent_RaisesEventRescheduled()
    {
        var concert = Given.PublishedConcert();
        concert.ClearDomainEvents();

        concert.Reschedule(Given.OrganizerId, Given.Schedule(60), Given.Now);

        var raised = Assert.Single(concert.DomainEvents.OfType<EventRescheduledDomainEvent>());
        Assert.Equal(Given.Schedule(45), raised.PreviousSchedule);
        Assert.Equal(Given.Schedule(60), raised.NewSchedule);
    }

    [Fact]
    public void Reschedule_OfADraft_RaisesNothing()
    {
        var concert = Given.Concert();

        concert.Reschedule(Given.OrganizerId, Given.Schedule(60), Given.Now);

        Assert.Empty(concert.DomainEvents);
    }

    [Fact]
    public void MarkCompleted_BeforeTheEventIsOver_Throws()
    {
        var concert = Given.PublishedConcert();

        Assert.Throws<BusinessRuleViolationException>(() => concert.MarkCompleted(Given.Now));
    }

    [Fact]
    public void RunScheduledMaintenance_ClosesAnEventThatIsOver()
    {
        var concert = Given.PublishedConcert();

        concert.RunScheduledMaintenance(Given.Now.AddDays(46));

        Assert.Equal(EventStatus.Completed, concert.Status);
    }

    [Fact]
    public void RunScheduledMaintenance_LeavesAnUpcomingEventAlone()
    {
        var concert = Given.PublishedConcert();

        concert.RunScheduledMaintenance(Given.Now.AddDays(1));

        Assert.Equal(EventStatus.Published, concert.Status);
    }

    [Fact]
    public void OccupancyRate_IsDerivedFromTheAllocations()
    {
        var concert = Given.PublishedConcert(standardSeats: 100, vipSeats: 0);
        var reservation = concert.Reserve(Given.Order(concert, "Parter", 5), Given.Now);
        concert.ConfirmReservation(reservation);

        Assert.Equal(Percentage.Of(5m), concert.OccupancyRate);
    }

    [Fact]
    public void CheapestTicketPrice_IsTheLowestListPrice()
    {
        var concert = Given.PublishedConcert();

        Assert.Equal(Money.Of(40m), concert.CheapestTicketPrice);
    }
}
