using EventBooking.Application.Catalog;
using EventBooking.Application.Tests.TestKit;
using EventBooking.Domain.Events;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Tests;

public sealed class EventSearchTests
{
    [Fact]
    public void ByDefault_OnlyBookableEventsComeBack()
    {
        using var host = new TestHost();
        var onSale = host.PublishConcert(seats: 10);
        var draft = host.Catalog.CreateConcert(new CreateConcertRequest
        {
            Title = "Nacrt",
            Description = "Jos nije objavljen",
            Schedule = host.Schedule(daysAhead: 30),
            VenueId = host.AddVenue().Id,
            OrganizerId = host.AddOrganizer().Id,
            Headliner = "Headliner",
        });

        var results = host.Catalog.Search(new EventSearchCriteria());

        Assert.Contains(onSale, results);
        Assert.DoesNotContain(draft, results);
    }

    [Fact]
    public void SoldOutEventsDropOutOfTheDefaultView()
    {
        using var host = new TestHost();
        var concert = host.PublishConcert(seats: 1);
        var booking = host.Bookings.PlaceHold(
            host.AddCustomer().Id,
            concert.Id,
            TestHost.Order(concert, "Parter", 1));
        host.Bookings.Confirm(booking.Id);

        Assert.Empty(host.Catalog.Search(new EventSearchCriteria()));
        Assert.Single(host.Catalog.Search(new EventSearchCriteria { OnlyBookable = false }));
    }

    [Fact]
    public void FiltersCombineWithAnd()
    {
        using var host = new TestHost();
        host.PublishConcert(city: "Sarajevo");
        host.PublishConcert(city: "Mostar");
        host.PublishWorkshop();

        var results = host.Catalog.Search(new EventSearchCriteria
        {
            Category = EventCategory.Concert,
            City = "Mostar",
        });

        Assert.Equal("Mostar", Assert.Single(results).Venue.City);
    }

    [Fact]
    public void AnEmptyCriteriaObjectDoesNotFilterAnythingOut()
    {
        using var host = new TestHost();
        host.PublishConcert();
        host.PublishWorkshop();

        Assert.Equal(2, host.Catalog.Search(new EventSearchCriteria()).Count);
    }

    [Fact]
    public void TextSearchIsCaseInsensitive()
    {
        using var host = new TestHost();
        host.PublishConcert();
        host.PublishWorkshop();

        Assert.Single(host.Catalog.Search(new EventSearchCriteria { Text = "RADIONICA" }));
    }

    [Fact]
    public void BudgetFilterUsesTheCheapestTicket()
    {
        using var host = new TestHost();
        host.PublishConcert();
        host.PublishWorkshop();

        var affordable = host.Catalog.Search(new EventSearchCriteria { MaxPrice = Money.Of(50m) });

        Assert.Equal(EventCategory.Concert, Assert.Single(affordable).Category);
    }

    [Fact]
    public void DateFilterNarrowsToAWindow()
    {
        using var host = new TestHost();
        host.PublishConcert(daysAhead: 10);
        host.PublishConcert(daysAhead: 100);

        var soon = host.Catalog.Search(new EventSearchCriteria
        {
            To = host.Clock.UtcNow.AddDays(30),
        });

        Assert.Single(soon);
    }

    [Fact]
    public void ResultsAreSortedByStartDateByDefault()
    {
        using var host = new TestHost();
        var later = host.PublishConcert(daysAhead: 100);
        var sooner = host.PublishConcert(daysAhead: 10);

        var results = host.Catalog.Search(new EventSearchCriteria());

        Assert.Equal([sooner, later], results);
    }

    [Fact]
    public void ResultsCanBeSortedByPrice()
    {
        using var host = new TestHost();
        var expensive = host.PublishWorkshop();
        var cheap = host.PublishConcert();

        var results = host.Catalog.Search(new EventSearchCriteria { SortBy = EventSortOrder.CheapestFirst });

        Assert.Equal([cheap, expensive], results);
    }

    [Fact]
    public void ResultsCanBeSortedByPopularity()
    {
        using var host = new TestHost();
        var quiet = host.PublishConcert(seats: 50);
        var popular = host.PublishConcert(seats: 50);
        var booking = host.Bookings.PlaceHold(
            host.AddCustomer().Id,
            popular.Id,
            TestHost.Order(popular, "Parter", 5));
        host.Bookings.Confirm(booking.Id);

        var results = host.Catalog.Search(new EventSearchCriteria { SortBy = EventSortOrder.Popularity });

        Assert.Equal([popular, quiet], results);
    }
}
