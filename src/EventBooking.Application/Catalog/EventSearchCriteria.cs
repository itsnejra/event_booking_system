using EventBooking.Domain.Entities;
using EventBooking.Domain.Enums;
using EventBooking.Domain.Specifications;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Catalog;

/// <summary>
/// What the user typed into the search screen. Every field is optional; the criteria object knows how
/// to fold the ones that were filled in into a single specification.
/// </summary>
public sealed record EventSearchCriteria
{
    public string? Text { get; init; }

    public EventCategory? Category { get; init; }

    public string? City { get; init; }

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    public Money? MaxPrice { get; init; }

    /// <summary>Default view for a customer: published events that still have tickets.</summary>
    public bool OnlyBookable { get; init; } = true;

    public EventSortOrder SortBy { get; init; } = EventSortOrder.StartDate;

    public Specification<Event> ToSpecification()
    {
        var specification = Specifications.AlwaysTrue<Event>();

        if (OnlyBookable)
        {
            specification = specification.And(new BookableEventSpecification());
        }

        if (!string.IsNullOrWhiteSpace(Text))
        {
            specification = specification.And(new EventMatchingTextSpecification(Text));
        }

        if (Category is { } category)
        {
            specification = specification.And(new EventInCategorySpecification(category));
        }

        if (!string.IsNullOrWhiteSpace(City))
        {
            specification = specification.And(new EventAtCitySpecification(City));
        }

        if (From is not null || To is not null)
        {
            specification = specification.And(new EventStartingBetweenSpecification(From, To));
        }

        if (MaxPrice is { } budget)
        {
            specification = specification.And(new EventWithinBudgetSpecification(budget));
        }

        return specification;
    }

    public IOrderedEnumerable<Event> Sort(IEnumerable<Event> events) => SortBy switch
    {
        EventSortOrder.Title => events.OrderBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase),
        EventSortOrder.CheapestFirst => events.OrderBy(item => item.CheapestTicketPrice?.Amount ?? decimal.MaxValue),
        EventSortOrder.Popularity => events.OrderByDescending(item => item.TicketsSold),
        _ => events.OrderBy(item => item.Schedule.Start),
    };
}
