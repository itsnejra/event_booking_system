using EventBooking.Domain.Common;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Events;

/// <summary>
/// A customer who wants tickets for a sold out event. Being notified does not reserve anything -
/// it only tells them to hurry, which keeps the waiting list from silently spending their money.
/// </summary>
public sealed class WaitlistEntry : Entity<WaitlistEntryId>
{
    public WaitlistEntry(WaitlistEntryId id, UserId customerId, int requestedQuantity, DateTimeOffset joinedAt)
        : base(id)
    {
        CustomerId = customerId;
        RequestedQuantity = Guard.Positive(requestedQuantity);
        JoinedAt = joinedAt;
    }

    public UserId CustomerId { get; }

    public int RequestedQuantity { get; }

    public DateTimeOffset JoinedAt { get; }

    public DateTimeOffset? NotifiedAt { get; private set; }

    public bool IsWaiting => NotifiedAt is null;

    public void MarkNotified(DateTimeOffset moment)
    {
        if (NotifiedAt is not null)
        {
            return;
        }

        NotifiedAt = moment;
    }
}
