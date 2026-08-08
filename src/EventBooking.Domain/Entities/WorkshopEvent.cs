using EventBooking.Domain.Common;
using EventBooking.Domain.Enums;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.Interfaces;
using EventBooking.Domain.Refunds;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Entities;

/// <summary>
/// A hands-on workshop with one instructor. Small by nature, which drives every difference:
/// two tickets per booking so a single company cannot take the whole room, and a minimum number of
/// attendees below which running it makes no sense - checked automatically as the date approaches.
/// </summary>
public sealed class WorkshopEvent : Event
{
    /// <summary>How long before the start the "will this actually run?" decision is made.</summary>
    public static readonly TimeSpan ViabilityDeadline = TimeSpan.FromHours(48);

    public WorkshopEvent(
        EventId id,
        string title,
        string description,
        DateRange schedule,
        Venue venue,
        UserId organizerId,
        string instructor,
        int minimumAttendees,
        Currency currency = Currency.BAM)
        : base(id, title, description, schedule, venue, organizerId, currency)
    {
        Instructor = Guard.MaxLength(Guard.NotEmpty(instructor), 120);
        MinimumAttendees = Guard.Positive(minimumAttendees);
    }

    public string Instructor { get; }

    public int MinimumAttendees { get; }

    public bool IsViable => TicketsSold >= MinimumAttendees;

    public int SeatsUntilViable => Math.Max(0, MinimumAttendees - TicketsSold);

    public override EventCategory Category => EventCategory.Workshop;

    public override int MaxTicketsPerBooking => 2;

    public override IRefundPolicy RefundPolicy => RefundPolicies.FullUntil(TimeSpan.FromDays(2));

    protected override void OnValidatePublish()
    {
        if (MinimumAttendees > TotalCapacity)
        {
            throw new BusinessRuleViolationException(
                $"The workshop needs {MinimumAttendees} attendee(s) but only offers {TotalCapacity} seat(s).");
        }
    }

    /// <summary>
    /// Once the deadline passes, a workshop that has not filled up is called off automatically.
    /// Cancelling here means every affected booking is refunded in full through the normal path.
    /// </summary>
    protected override void OnScheduledMaintenance(DateTimeOffset now)
    {
        if (Schedule.NoticeBefore(now) > ViabilityDeadline || IsViable)
        {
            return;
        }

        // CancelCore, not Cancel: the system calls the workshop off, so there is nobody to authorise.
        CancelCore(
            $"Only {TicketsSold} of the required {MinimumAttendees} attendee(s) signed up.",
            now);
    }

    public override string ToString() => $"{Title} ({Instructor}) - {Schedule} @ {Venue}";
}
