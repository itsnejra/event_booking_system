using EventBooking.Domain.Common;
using EventBooking.Domain.Enums;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.Interfaces;
using EventBooking.Domain.Refunds;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Entities;

/// <summary>
/// A concert: large, one-off, and the kind of event people resell tickets for. Hence the two rules
/// that make it different - a cap on VIP tickets per booking to keep them out of the hands of
/// touts, and a refund schedule that gets stricter as the date approaches.
/// </summary>
public sealed class ConcertEvent : Event
{
    public const int MaxVipTicketsPerBooking = 2;

    public ConcertEvent(
        EventId id,
        string title,
        string description,
        DateRange schedule,
        Venue venue,
        UserId organizerId,
        string headliner,
        Currency currency = Currency.BAM)
        : base(id, title, description, schedule, venue, organizerId, currency)
    {
        Headliner = Guard.MaxLength(Guard.NotEmpty(headliner), 120);
    }

    public string Headliner { get; }

    public override EventCategory Category => EventCategory.Concert;

    public override int MaxTicketsPerBooking => 6;

    public override IRefundPolicy RefundPolicy => RefundPolicies.Graduated();

    protected override void OnValidateReservation(IReadOnlyList<ReservedTicketLine> lines, DateTimeOffset now)
    {
        var vipTickets = lines.Where(line => line.Tier == TicketTier.Vip).Sum(line => line.Quantity);

        if (vipTickets > MaxVipTicketsPerBooking)
        {
            throw new BusinessRuleViolationException(
                $"At most {MaxVipTicketsPerBooking} VIP ticket(s) per booking, {vipTickets} requested.");
        }
    }

    protected override void OnValidatePublish()
    {
        if (!TicketTypes.Any(type => type.Tier == TicketTier.Standard))
        {
            throw new BusinessRuleViolationException(
                "A concert must offer at least one standard ticket type before it goes on sale.");
        }
    }

    public override string ToString() => $"{Title} ({Headliner}) - {Schedule} @ {Venue}";
}
