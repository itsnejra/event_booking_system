using EventBooking.Domain.Abstractions;
using EventBooking.Domain.Events;
using EventBooking.Domain.Events.Specifications;
using EventBooking.Domain.Tests.TestKit;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Tests.Specifications;

public sealed class SpecificationTests
{
    [Fact]
    public void And_RequiresBothSides()
    {
        var specification = new EventInCategorySpecification(EventCategory.Concert)
            .And(new EventWithStatusSpecification(EventStatus.Published));

        Assert.True(specification.IsSatisfiedBy(Given.PublishedConcert()));
        Assert.False(specification.IsSatisfiedBy(Given.Concert()));
        Assert.False(specification.IsSatisfiedBy(Given.PublishedConference()));
    }

    [Fact]
    public void Or_AcceptsEitherSide()
    {
        var specification = new EventInCategorySpecification(EventCategory.Concert)
            .Or(new EventInCategorySpecification(EventCategory.Workshop));

        Assert.True(specification.IsSatisfiedBy(Given.Concert()));
        Assert.True(specification.IsSatisfiedBy(Given.Workshop()));
        Assert.False(specification.IsSatisfiedBy(Given.Conference()));
    }

    [Fact]
    public void Not_Inverts()
    {
        var specification = new EventInCategorySpecification(EventCategory.Concert).Not();

        Assert.False(specification.IsSatisfiedBy(Given.Concert()));
        Assert.True(specification.IsSatisfiedBy(Given.Workshop()));
    }

    [Fact]
    public void Operators_ReadTheSameAsTheMethods()
    {
        var concerts = new EventInCategorySpecification(EventCategory.Concert);
        var published = new EventWithStatusSpecification(EventStatus.Published);

        Assert.True((concerts & published).IsSatisfiedBy(Given.PublishedConcert()));
        Assert.True((!concerts).IsSatisfiedBy(Given.Workshop()));
    }

    [Fact]
    public void AlwaysTrue_IsTheNeutralElementOfAnd()
    {
        var concerts = new EventInCategorySpecification(EventCategory.Concert);

        // Fully qualified: this test's own namespace also ends in "Specifications".
        var folded = Domain.Abstractions.Specifications.AlwaysTrue<Event>().And(concerts);

        Assert.Same(concerts, folded);
    }

    [Fact]
    public void TextSearch_LooksAtTitleDescriptionAndVenue()
    {
        var concert = Given.PublishedConcert();

        Assert.True(new EventMatchingTextSpecification("koncert").IsSatisfiedBy(concert));
        Assert.True(new EventMatchingTextSpecification("SARAJEVO").IsSatisfiedBy(concert));
        Assert.True(new EventMatchingTextSpecification("dvorana").IsSatisfiedBy(concert));
        Assert.False(new EventMatchingTextSpecification("mostar").IsSatisfiedBy(concert));
    }

    [Fact]
    public void StartingBetween_TreatsBothEndsAsOptional()
    {
        var concert = Given.Concert(daysAhead: 45);

        Assert.True(new EventStartingBetweenSpecification(Given.Now, null).IsSatisfiedBy(concert));
        Assert.True(new EventStartingBetweenSpecification(null, Given.Now.AddDays(50)).IsSatisfiedBy(concert));
        Assert.False(new EventStartingBetweenSpecification(null, Given.Now.AddDays(10)).IsSatisfiedBy(concert));
    }

    [Fact]
    public void WithinBudget_ComparesAgainstTheCheapestTicket()
    {
        var concert = Given.PublishedConcert();

        Assert.True(new EventWithinBudgetSpecification(Money.Of(40m)).IsSatisfiedBy(concert));
        Assert.False(new EventWithinBudgetSpecification(Money.Of(39m)).IsSatisfiedBy(concert));
    }

    [Fact]
    public void WithinBudget_IgnoresEventsPricedInAnotherCurrency()
    {
        var concert = Given.PublishedConcert();

        Assert.False(new EventWithinBudgetSpecification(Money.Of(500m, Currency.EUR)).IsSatisfiedBy(concert));
    }

    [Fact]
    public void WithinBudget_IgnoresEventsThatHaveNoTicketsYet()
    {
        Assert.False(new EventWithinBudgetSpecification(Money.Of(1000m)).IsSatisfiedBy(Given.Concert()));
    }

    [Fact]
    public void Bookable_MeansPublishedAndNotSoldOut()
    {
        var concert = Given.PublishedConcert(standardSeats: 1, vipSeats: 0);
        var specification = new BookableEventSpecification();

        Assert.True(specification.IsSatisfiedBy(concert));

        concert.Reserve(Given.Order(concert, "Parter", 1), Given.Now);

        Assert.False(specification.IsSatisfiedBy(concert));
        Assert.False(specification.IsSatisfiedBy(Given.Concert()));
    }
}
