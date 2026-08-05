namespace EventBooking.Domain.Events;

/// <summary>
/// Coarse classification used for browsing and reporting. It mirrors the <c>Event</c> subclasses,
/// but exists separately so that filtering and grouping never have to reflect over types.
/// </summary>
public enum EventCategory
{
    Concert = 0,
    Conference = 1,
    Workshop = 2,
}
