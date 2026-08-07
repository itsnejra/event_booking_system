using EventBooking.Domain.Common;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.Refunds;
using EventBooking.Domain.ValueObjects;
using EventBooking.Domain.Venues;

namespace EventBooking.Domain.Events;

/// <summary>
/// A multi-day conference. It owns a programme of sessions, which brings its own invariant - two
/// talks cannot occupy the same track at the same time - and it expects corporate group bookings,
/// so the per-booking limit is much higher than for a concert.
/// </summary>
public sealed class ConferenceEvent : Event
{
    private readonly List<ConferenceSession> _sessions = [];

    public ConferenceEvent(
        EventId id,
        string title,
        string description,
        DateRange schedule,
        Venue venue,
        UserId organizerId,
        string topic,
        Currency currency = Currency.BAM)
        : base(id, title, description, schedule, venue, organizerId, currency)
    {
        Topic = Guard.MaxLength(Guard.NotEmpty(topic), 120);
    }

    public string Topic { get; }

    public IReadOnlyList<ConferenceSession> Sessions => _sessions.AsReadOnly();

    public IReadOnlyCollection<string> Tracks =>
        [.. _sessions.Select(session => session.Track).Distinct(StringComparer.OrdinalIgnoreCase).Order()];

    public override EventCategory Category => EventCategory.Conference;

    /// <summary>Companies buy for whole teams, so a single booking may cover twenty people.</summary>
    public override int MaxTicketsPerBooking => 20;

    /// <summary>
    /// Travel and hotels are booked around a conference, so the cut-off is generous but absolute.
    /// </summary>
    public override IRefundPolicy RefundPolicy => RefundPolicies.FullUntil(TimeSpan.FromDays(7));

    public void AddSession(UserId actingOrganizer, ConferenceSession session)
    {
        EnsureManagedBy(actingOrganizer);
        Guard.NotNull(session);

        if (Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            throw new InvalidStateTransitionException(Describe(), Status, "add sessions to");
        }

        if (session.Slot.Start < Schedule.Start || session.Slot.End > Schedule.End)
        {
            throw new BusinessRuleViolationException(
                $"Session '{session.Title}' falls outside the conference schedule ({Schedule}).");
        }

        var clash = _sessions.FirstOrDefault(existing =>
            string.Equals(existing.Track, session.Track, StringComparison.OrdinalIgnoreCase)
            && existing.Slot.Overlaps(session.Slot));

        if (clash is not null)
        {
            throw new BusinessRuleViolationException(
                $"Track '{session.Track}' already has '{clash.Title}' scheduled at {clash.Slot}.");
        }

        _sessions.Add(session);
    }

    protected override void OnValidatePublish()
    {
        if (_sessions.Count == 0)
        {
            throw new BusinessRuleViolationException(
                "A conference needs at least one session in its programme before it goes on sale.");
        }
    }

    public override string ToString() => $"{Title} ({Topic}) - {Schedule} @ {Venue}";
}
