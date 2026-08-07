using EventBooking.Domain.Bookings;
using EventBooking.Domain.Common;
using EventBooking.Domain.Events;
using EventBooking.Domain.Pricing;
using EventBooking.Domain.Pricing.Rules;
using EventBooking.Domain.Tests.TestKit;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Tests.Common;

/// <summary>
/// Declaring a property as <see cref="IReadOnlyList{T}"/> and returning the private list itself
/// protects nothing: the caller casts it back to <c>List&lt;T&gt;</c> and edits the inside of the
/// aggregate without going through any of its rules. These tests lock the collections down.
/// </summary>
public sealed class AggregateEncapsulationTests
{
    [Fact]
    public void TicketTypes_CannotBeCastBackToTheUnderlyingList()
    {
        var concert = Given.PublishedConcert();

        Assert.Throws<InvalidCastException>(() => (List<TicketType>)concert.TicketTypes);
    }

    [Fact]
    public void Waitlist_CannotBeCastBackToTheUnderlyingList()
    {
        var concert = Given.PublishedConcert(standardSeats: 1, vipSeats: 0);
        concert.Reserve(Given.Order(concert, "Parter", 1), Given.Now);
        concert.JoinWaitlist(UserId.New(), 1, Given.Now);

        Assert.Throws<InvalidCastException>(() => (List<WaitlistEntry>)concert.Waitlist);
    }

    [Fact]
    public void Sessions_CannotBeCastBackToTheUnderlyingList()
    {
        var conference = Given.PublishedConference();

        Assert.Throws<InvalidCastException>(() => (List<ConferenceSession>)conference.Sessions);
    }

    [Fact]
    public void DomainEvents_CannotBeCastBackToTheUnderlyingList()
    {
        var concert = Given.PublishedConcert();

        Assert.Throws<InvalidCastException>(() => (List<IDomainEvent>)concert.DomainEvents);
    }

    [Fact]
    public void BookingLines_CannotBeCastBackToTheUnderlyingList()
    {
        var booking = APendingBooking();

        Assert.Throws<InvalidCastException>(() => (List<BookingLine>)booking.Lines);
    }

    [Fact]
    public void PricingRules_CannotBeCastBackToTheUnderlyingList()
    {
        var engine = new PricingEngine([EarlyBirdDiscountRule.Default]);

        Assert.Throws<InvalidCastException>(() => (List<IPricingRule>)engine.Rules);
    }

    /// <summary>The seats an aggregate reports are still the seats it has - the fix hides nothing.</summary>
    [Fact]
    public void TheExposedCollectionsStillShowTheRealContents()
    {
        var concert = Given.PublishedConcert(standardSeats: 40, vipSeats: 10);

        Assert.Equal(2, concert.TicketTypes.Count);
        Assert.Equal(50, concert.TotalCapacity);
        Assert.Contains(concert.TicketTypes, type => type.Name == "VIP");
    }

    private static Booking APendingBooking()
    {
        var concert = Given.PublishedConcert(daysAhead: 45);
        var reservation = concert.Reserve(Given.Order(concert, "Parter", 1), Given.Now);
        var priced = new PricingEngine([EarlyBirdDiscountRule.Default])
            .Price(concert, Given.Customer(), reservation, Given.Now);

        return Booking.PlaceHold(
            BookingId.New(),
            BookingReference.For(Given.Now, 1),
            UserId.New(),
            priced,
            Given.Now);
    }
}
