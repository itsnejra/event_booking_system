using EventBooking.Domain.Exceptions;
using EventBooking.Domain.Tests.TestKit;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Tests.Events;

public sealed class WaitlistTests
{
    [Fact]
    public void JoinWaitlist_WhileSeatsAreStillAvailable_Throws()
    {
        var concert = Given.PublishedConcert(standardSeats: 10, vipSeats: 0);

        Assert.Throws<BusinessRuleViolationException>(
            () => concert.JoinWaitlist(UserId.New(), 2, Given.Now));
    }

    [Fact]
    public void JoinWaitlist_OnceSoldOut_IsAllowed()
    {
        var concert = SoldOutConcert();

        var entry = concert.JoinWaitlist(UserId.New(), 2, Given.Now);

        Assert.True(entry.IsWaiting);
        Assert.Single(concert.Waitlist);
    }

    [Fact]
    public void JoinWaitlist_Twice_Throws()
    {
        var concert = SoldOutConcert();
        var customer = UserId.New();
        concert.JoinWaitlist(customer, 2, Given.Now);

        Assert.Throws<BusinessRuleViolationException>(() => concert.JoinWaitlist(customer, 1, Given.Now));
    }

    [Fact]
    public void TakeWaitlistCandidates_NotifiesInTheOrderPeopleJoined()
    {
        var concert = SoldOutConcert();
        var first = UserId.New();
        var second = UserId.New();
        concert.JoinWaitlist(first, 1, Given.Now);
        concert.JoinWaitlist(second, 1, Given.Now.AddMinutes(5));

        var notified = concert.TakeWaitlistCandidates(1, Given.Now.AddHours(1));

        Assert.Equal(first, Assert.Single(notified).CustomerId);
    }

    [Fact]
    public void TakeWaitlistCandidates_SkipsRequestsLargerThanWhatCameBack()
    {
        var concert = SoldOutConcert();
        var wantsThree = UserId.New();
        var wantsOne = UserId.New();
        concert.JoinWaitlist(wantsThree, 3, Given.Now);
        concert.JoinWaitlist(wantsOne, 1, Given.Now.AddMinutes(1));

        var notified = concert.TakeWaitlistCandidates(1, Given.Now.AddHours(1));

        Assert.Equal(wantsOne, Assert.Single(notified).CustomerId);
    }

    [Fact]
    public void TakeWaitlistCandidates_DoesNotNotifyTheSamePersonTwice()
    {
        var concert = SoldOutConcert();
        concert.JoinWaitlist(UserId.New(), 1, Given.Now);

        concert.TakeWaitlistCandidates(1, Given.Now.AddHours(1));
        var second = concert.TakeWaitlistCandidates(1, Given.Now.AddHours(2));

        Assert.Empty(second);
    }

    [Fact]
    public void TakeWaitlistCandidates_StopsOnceTheReleasedSeatsAreUsedUp()
    {
        var concert = SoldOutConcert();
        concert.JoinWaitlist(UserId.New(), 2, Given.Now);
        concert.JoinWaitlist(UserId.New(), 2, Given.Now.AddMinutes(1));
        concert.JoinWaitlist(UserId.New(), 2, Given.Now.AddMinutes(2));

        var notified = concert.TakeWaitlistCandidates(4, Given.Now.AddHours(1));

        Assert.Equal(2, notified.Count);
    }

    private static Domain.Events.ConcertEvent SoldOutConcert()
    {
        var concert = Given.PublishedConcert(standardSeats: 2, vipSeats: 0);
        concert.ConfirmReservation(concert.Reserve(Given.Order(concert, "Parter", 2), Given.Now));
        return concert;
    }
}
