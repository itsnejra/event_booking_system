using EventBooking.Domain.Common;
using EventBooking.Domain.Events;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.Pricing;
using EventBooking.Domain.Refunds;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Bookings;

/// <summary>
/// A customer's claim on a set of tickets, and the record of what they agreed to pay for them.
/// </summary>
/// <remarks>
/// A booking starts as a time-limited hold. That is what makes the flow honest: seats leave the pool
/// the moment somebody starts checking out, and come back on their own if the checkout is abandoned.
/// The booking never touches the event's inventory itself - the two aggregates are kept separate,
/// and <c>BookingService</c> is the one place that moves them together.
/// </remarks>
public sealed class Booking : AggregateRoot<BookingId>
{
    public static readonly TimeSpan DefaultHoldDuration = TimeSpan.FromMinutes(15);

    private readonly List<BookingLine> _lines;

    private Booking(
        BookingId id,
        BookingReference reference,
        UserId customerId,
        EventId eventId,
        List<BookingLine> lines,
        DateTimeOffset createdAt,
        DateTimeOffset holdExpiresAt)
        : base(id)
    {
        Reference = reference;
        CustomerId = customerId;
        EventId = eventId;
        _lines = lines;
        CreatedAt = createdAt;
        HoldExpiresAt = holdExpiresAt;
    }

    public BookingReference Reference { get; }

    public UserId CustomerId { get; }

    public EventId EventId { get; }

    public IReadOnlyList<BookingLine> Lines => _lines;

    public BookingStatus Status { get; private set; } = BookingStatus.Pending;

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset HoldExpiresAt { get; }

    public DateTimeOffset? ConfirmedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public string? CancellationReason { get; private set; }

    public Money? RefundAmount { get; private set; }

    public int TotalTickets => _lines.Sum(line => line.Quantity);

    public Currency Currency => _lines[0].UnitPrice.Currency;

    public Money Subtotal => Money.Sum(_lines.Select(line => line.Subtotal), Currency);

    public Money DiscountTotal => Money.Sum(_lines.Select(line => line.Discount), Currency);

    public Money Total => Money.Sum(_lines.Select(line => line.Total), Currency);

    /// <summary>True while the booking still holds seats - pending or paid for.</summary>
    public bool HoldsSeats => Status is BookingStatus.Pending or BookingStatus.Confirmed;

    /// <summary>Whether the money has actually been taken. Drives which inventory bucket to return seats to.</summary>
    public bool IsPaidFor => Status == BookingStatus.Confirmed;

    public bool HasExpired(DateTimeOffset now) => Status == BookingStatus.Pending && now >= HoldExpiresAt;

    public static Booking PlaceHold(
        BookingId id,
        BookingReference reference,
        UserId customerId,
        PricedOrder order,
        DateTimeOffset now,
        TimeSpan? holdDuration = null)
    {
        Guard.NotNull(reference);
        Guard.NotNull(order);

        if (order.Lines.Count == 0)
        {
            throw new BusinessRuleViolationException("A booking must contain at least one line.");
        }

        var duration = Guard.PositiveDuration(holdDuration ?? DefaultHoldDuration);
        var lines = order.Lines.Select(BookingLine.From).ToList();
        var booking = new Booking(id, reference, customerId, order.EventId, lines, now, now + duration);

        booking.RaiseDomainEvent(new BookingPlacedDomainEvent(
            id,
            reference,
            customerId,
            order.EventId,
            booking.Total,
            booking.HoldExpiresAt,
            now));

        return booking;
    }

    public void Confirm(DateTimeOffset now)
    {
        if (Status != BookingStatus.Pending)
        {
            throw new InvalidStateTransitionException(Describe(), Status, "confirm");
        }

        if (now >= HoldExpiresAt)
        {
            throw new BusinessRuleViolationException(
                $"The hold on {Describe()} expired at {HoldExpiresAt:dd.MM.yyyy HH:mm}.");
        }

        Status = BookingStatus.Confirmed;
        ConfirmedAt = now;
        RaiseDomainEvent(new BookingConfirmedDomainEvent(Id, Reference, CustomerId, EventId, Total, now));
    }

    /// <summary>Lets an unpaid hold lapse. Idempotent, because a sweep may run more than once.</summary>
    public bool Expire(DateTimeOffset now)
    {
        if (!HasExpired(now))
        {
            return false;
        }

        Status = BookingStatus.Expired;
        RaiseDomainEvent(new BookingExpiredDomainEvent(Id, Reference, CustomerId, EventId, now));
        return true;
    }

    /// <summary>
    /// Cancels the booking and works out what goes back to the customer. The refund rule comes from
    /// the event, so the booking never needs to know what kind of event it belongs to.
    /// </summary>
    /// <returns>The amount refunded - zero for a booking that was never paid for.</returns>
    public Money Cancel(IRefundPolicy refundPolicy, DateTimeOffset eventStart, DateTimeOffset now, string reason)
    {
        Guard.NotNull(refundPolicy);

        if (!HoldsSeats)
        {
            throw new InvalidStateTransitionException(Describe(), Status, "cancel");
        }

        var refund = IsPaidFor
            ? refundPolicy.CalculateRefund(Total, eventStart, now)
            : Money.Zero(Currency);

        ApplyCancellation(refund, reason, now);
        return refund;
    }

    /// <summary>
    /// The organiser called the event off. The customer keeps none of the risk, so the refund rule
    /// of the event does not apply - they get everything back.
    /// </summary>
    public Money CancelBecauseEventCancelled(DateTimeOffset now, string reason)
    {
        if (!HoldsSeats)
        {
            throw new InvalidStateTransitionException(Describe(), Status, "cancel");
        }

        var refund = IsPaidFor ? FullRefundPolicy.Instance.CalculateRefund(Total, now, now) : Money.Zero(Currency);
        ApplyCancellation(refund, reason, now);
        return refund;
    }

    /// <summary>
    /// Rebuilds the reservation this booking represents, so the event can take its seats back.
    /// </summary>
    public TicketReservation AsReservation() => new(
        EventId,
        [.. _lines.Select(line => new ReservedTicketLine(
            line.TicketTypeId,
            line.TicketTypeName,
            line.Tier,
            line.UnitPrice,
            line.Quantity))]);

    public override string ToString() => $"{Reference} - {TotalTickets} ticket(s), {Total} ({Status})";

    private void ApplyCancellation(Money refund, string reason, DateTimeOffset now)
    {
        var wasPaidFor = IsPaidFor;

        Status = BookingStatus.Cancelled;
        CancelledAt = now;
        CancellationReason = Guard.MaxLength(Guard.NotEmpty(reason), 500);
        RefundAmount = refund;

        RaiseDomainEvent(new BookingCancelledDomainEvent(
            Id,
            Reference,
            CustomerId,
            EventId,
            refund,
            CancellationReason,
            wasPaidFor,
            now));
    }

    private string Describe() => $"booking {Reference}";
}
