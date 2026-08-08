using EventBooking.Domain.Entities;
using EventBooking.Domain.Enums;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Specifications;

// The family of filters the catalogue understands. They live in one file because they are one
// vocabulary and each is a couple of lines; anything with real logic in it would get its own file.
// Search screens combine these instead of writing their own predicates - see EventSearchCriteria.

public sealed class EventInCategorySpecification(EventCategory category) : Specification<Event>
{
    public override bool IsSatisfiedBy(Event candidate) => candidate.Category == category;
}

public sealed class EventWithStatusSpecification(EventStatus status) : Specification<Event>
{
    public override bool IsSatisfiedBy(Event candidate) => candidate.Status == status;
}

public sealed class EventAtCitySpecification(string city) : Specification<Event>
{
    public override bool IsSatisfiedBy(Event candidate) =>
        string.Equals(candidate.Venue.City, city, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Events whose start falls inside the given window; either end may be left open.</summary>
public sealed class EventStartingBetweenSpecification(DateTimeOffset? from, DateTimeOffset? to) : Specification<Event>
{
    public override bool IsSatisfiedBy(Event candidate) =>
        (from is null || candidate.Schedule.Start >= from.Value)
        && (to is null || candidate.Schedule.Start <= to.Value);
}

/// <summary>Free-text match over the fields a customer would actually search by.</summary>
public sealed class EventMatchingTextSpecification(string text) : Specification<Event>
{
    private readonly string _text = text.Trim();

    public override bool IsSatisfiedBy(Event candidate) =>
        Contains(candidate.Title)
        || Contains(candidate.Description)
        || Contains(candidate.Venue.Name)
        || Contains(candidate.Venue.City)
        || Contains(candidate.Category.ToString());

    private bool Contains(string value) => value.Contains(_text, StringComparison.OrdinalIgnoreCase);
}

public sealed class EventWithAvailableTicketsSpecification(int minimum = 1) : Specification<Event>
{
    public override bool IsSatisfiedBy(Event candidate) => candidate.AvailableTickets >= minimum;
}

/// <summary>Events whose cheapest ticket is within budget. Events with no tickets yet never match.</summary>
public sealed class EventWithinBudgetSpecification(Money budget) : Specification<Event>
{
    public override bool IsSatisfiedBy(Event candidate) =>
        candidate.CheapestTicketPrice is { } cheapest
        && cheapest.Currency == budget.Currency
        && cheapest <= budget;
}

/// <summary>Everything a customer is allowed to book right now.</summary>
public sealed class BookableEventSpecification : Specification<Event>
{
    public override bool IsSatisfiedBy(Event candidate) => candidate.IsBookable;
}
