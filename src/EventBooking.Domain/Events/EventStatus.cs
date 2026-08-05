namespace EventBooking.Domain.Events;

/// <summary>
/// Lifecycle of an event: Draft -> Published -> Completed, with Cancelled reachable from either of
/// the first two. "Sold out" is deliberately not a status - it is a fact derived from the ticket
/// allocations, and storing it would just be one more thing that can go stale.
/// </summary>
public enum EventStatus
{
    Draft = 0,
    Published = 1,
    Cancelled = 2,
    Completed = 3,
}
