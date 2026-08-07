using EventBooking.Domain.Common;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.Refunds;
using EventBooking.Domain.ValueObjects;
using EventBooking.Domain.Venues;

namespace EventBooking.Domain.Events;

/// <summary>
/// An event is the consistency boundary around everything that can be booked: its schedule, its
/// ticket types and their inventory, and its waiting list. Seats are only ever taken or returned
/// through this class, which is what makes overselling impossible by construction rather than by
/// discipline.
/// </summary>
/// <remarks>
/// The base class owns everything that is true of every event. What differs between a concert, a
/// conference and a workshop is expressed through three extension points - <see cref="Category"/>,
/// <see cref="MaxTicketsPerBooking"/> and <see cref="RefundPolicy"/> - plus two optional hooks,
/// <see cref="OnValidateReservation"/> and <see cref="OnValidatePublish"/>. Adding a fourth kind of
/// event means adding one class; it does not mean touching this one.
/// </remarks>
public abstract class Event : AggregateRoot<EventId>
{
    private readonly List<TicketType> _ticketTypes = [];
    private readonly List<WaitlistEntry> _waitlist = [];

    protected Event(
        EventId id,
        string title,
        string description,
        DateRange schedule,
        Venue venue,
        UserId organizerId,
        Currency currency = Currency.BAM)
        : base(id)
    {
        Title = Guard.MaxLength(Guard.NotEmpty(title), 150);
        Description = Guard.MaxLength(Guard.NotEmpty(description), 2000);
        Schedule = Guard.NotNull(schedule);
        Venue = Guard.NotNull(venue);
        OrganizerId = organizerId;
        Currency = currency;
    }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public DateRange Schedule { get; private set; }

    public Venue Venue { get; }

    public UserId OrganizerId { get; }

    public Currency Currency { get; }

    public EventStatus Status { get; private set; } = EventStatus.Draft;

    public string? CancellationReason { get; private set; }

    public IReadOnlyList<TicketType> TicketTypes => _ticketTypes.AsReadOnly();

    public IReadOnlyList<WaitlistEntry> Waitlist => _waitlist.AsReadOnly();

    // --- what the subclasses decide -------------------------------------------------------------

    public abstract EventCategory Category { get; }

    /// <summary>Upper bound on a single booking - a workshop of twelve people is not a stadium.</summary>
    public abstract int MaxTicketsPerBooking { get; }

    public abstract IRefundPolicy RefundPolicy { get; }

    // --- derived facts ---------------------------------------------------------------------------

    public int TotalCapacity => _ticketTypes.Sum(type => type.Allocation.Capacity);

    public int TicketsSold => _ticketTypes.Sum(type => type.Allocation.Sold);

    public int TicketsOnHold => _ticketTypes.Sum(type => type.Allocation.Reserved);

    public int AvailableTickets => _ticketTypes.Sum(type => type.Allocation.Available);

    public bool IsSoldOut => _ticketTypes.Count > 0 && AvailableTickets == 0;

    public bool IsBookable => Status == EventStatus.Published && !IsSoldOut;

    public Percentage OccupancyRate => TotalCapacity == 0
        ? Percentage.Zero
        : Percentage.Of(decimal.Round(TicketsSold * 100m / TotalCapacity, 2));

    public Money? CheapestTicketPrice => _ticketTypes.Count == 0
        ? null
        : _ticketTypes.Min(type => type.BasePrice);

    // --- catalogue management ---------------------------------------------------------------------

    /// <summary>
    /// Adds a ticket type. Allowed while the event is a draft and after it is published (organisers
    /// really do add a second batch), but never once it is cancelled or over.
    /// </summary>
    public TicketType AddTicketType(
        UserId actingOrganizer,
        string name,
        TicketTier tier,
        Money price,
        int capacity,
        DateRange? salesWindow = null)
    {
        EnsureManagedBy(actingOrganizer);

        if (Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            throw new InvalidStateTransitionException(Describe(), Status, "add ticket types to");
        }

        var trimmedName = Guard.NotEmpty(name);
        if (_ticketTypes.Any(type => string.Equals(type.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessRuleViolationException($"{Describe()} already has a ticket type called '{trimmedName}'.");
        }

        if (price.Currency != Currency)
        {
            throw new CurrencyMismatchException(Currency, price.Currency);
        }

        if (TotalCapacity + capacity > Venue.Capacity)
        {
            throw new BusinessRuleViolationException(
                $"{Venue.Name} holds {Venue.Capacity} people; {TotalCapacity + capacity} tickets would be too many.");
        }

        if (salesWindow is not null && salesWindow.End > Schedule.Start)
        {
            throw new BusinessRuleViolationException("Ticket sales must close no later than the start of the event.");
        }

        var ticketType = new TicketType(TicketTypeId.New(), trimmedName, tier, price, capacity, salesWindow);
        _ticketTypes.Add(ticketType);
        return ticketType;
    }

    public TicketType GetTicketType(TicketTypeId ticketTypeId) =>
        _ticketTypes.FirstOrDefault(type => type.Id == ticketTypeId)
        ?? throw EntityNotFoundException.For<TicketType>(ticketTypeId);

    public TicketType? FindTicketType(string name) =>
        _ticketTypes.FirstOrDefault(type => string.Equals(type.Name, name, StringComparison.OrdinalIgnoreCase));

    public void UpdateDetails(UserId actingOrganizer, string title, string description)
    {
        EnsureManagedBy(actingOrganizer);
        EnsureNotFinished("edit");
        Title = Guard.MaxLength(Guard.NotEmpty(title), 150);
        Description = Guard.MaxLength(Guard.NotEmpty(description), 2000);
    }

    // --- lifecycle ---------------------------------------------------------------------------------

    public void Publish(UserId actingOrganizer, DateTimeOffset now)
    {
        EnsureManagedBy(actingOrganizer);

        if (Status != EventStatus.Draft)
        {
            throw new InvalidStateTransitionException(Describe(), Status, "publish");
        }

        if (_ticketTypes.Count == 0)
        {
            throw new BusinessRuleViolationException($"{Describe()} has no ticket types and cannot go on sale.");
        }

        if (!Schedule.StartsAfter(now))
        {
            throw new BusinessRuleViolationException($"{Describe()} starts in the past and cannot be published.");
        }

        OnValidatePublish();

        Status = EventStatus.Published;
        RaiseDomainEvent(new EventPublishedDomainEvent(Id, Title, now));
    }

    public void Reschedule(UserId actingOrganizer, DateRange newSchedule, DateTimeOffset now)
    {
        EnsureManagedBy(actingOrganizer);
        Guard.NotNull(newSchedule);
        EnsureNotFinished("reschedule");

        if (!newSchedule.StartsAfter(now))
        {
            throw new BusinessRuleViolationException("A new date must be in the future.");
        }

        var previous = Schedule;
        Schedule = newSchedule;

        if (Status == EventStatus.Published)
        {
            RaiseDomainEvent(new EventRescheduledDomainEvent(Id, Title, previous, newSchedule, now));
        }
    }

    public void Cancel(UserId actingOrganizer, string reason, DateTimeOffset now)
    {
        EnsureManagedBy(actingOrganizer);
        CancelCore(reason, now);
    }

    /// <summary>
    /// Cancellation without an authorisation check, for when the system itself calls the event off
    /// rather than a person - see <see cref="WorkshopEvent"/>. Protected so that only the aggregate
    /// can reach it; a service cannot use this to slip past <see cref="EnsureManagedBy"/>.
    /// </summary>
    protected void CancelCore(string reason, DateTimeOffset now)
    {
        EnsureNotFinished("cancel");

        CancellationReason = Guard.MaxLength(Guard.NotEmpty(reason), 500);
        Status = EventStatus.Cancelled;
        RaiseDomainEvent(new EventCancelledDomainEvent(Id, Title, CancellationReason, now));
    }

    public void MarkCompleted(DateTimeOffset now)
    {
        if (Status != EventStatus.Published)
        {
            throw new InvalidStateTransitionException(Describe(), Status, "complete");
        }

        if (!Schedule.HasEnded(now))
        {
            throw new BusinessRuleViolationException($"{Describe()} has not finished yet.");
        }

        Status = EventStatus.Completed;
    }

    /// <summary>
    /// Housekeeping that a scheduler would run periodically. The base class only knows that an
    /// event which is over is over; anything else is up to the subclass.
    /// </summary>
    public void RunScheduledMaintenance(DateTimeOffset now)
    {
        if (Status != EventStatus.Published)
        {
            return;
        }

        if (Schedule.HasEnded(now))
        {
            MarkCompleted(now);
            return;
        }

        OnScheduledMaintenance(now);
    }

    // --- inventory -----------------------------------------------------------------------------------

    /// <summary>
    /// Holds seats for an order. Every line is validated before a single seat is taken, so a request
    /// that fails on its third line does not leave the first two silently reserved.
    /// </summary>
    public TicketReservation Reserve(IReadOnlyCollection<TicketOrderItem> items, DateTimeOffset now)
    {
        Guard.NotNull(items);

        if (items.Count == 0)
        {
            throw new BusinessRuleViolationException("A booking must contain at least one ticket.");
        }

        if (Status != EventStatus.Published)
        {
            throw new InvalidStateTransitionException(Describe(), Status, "book tickets for");
        }

        if (!Schedule.StartsAfter(now))
        {
            throw new BusinessRuleViolationException($"{Describe()} has already started.");
        }

        var requested = Consolidate(items);
        var totalQuantity = requested.Sum(item => item.Quantity);

        if (totalQuantity > MaxTicketsPerBooking)
        {
            throw new BusinessRuleViolationException(
                $"{Describe()} allows at most {MaxTicketsPerBooking} ticket(s) per booking, {totalQuantity} requested.");
        }

        var lines = new List<ReservedTicketLine>(requested.Count);
        foreach (var item in requested)
        {
            var ticketType = GetTicketType(item.TicketTypeId);
            ticketType.EnsureCanReserve(item.Quantity, now);
            lines.Add(new ReservedTicketLine(
                ticketType.Id,
                ticketType.Name,
                ticketType.Tier,
                ticketType.BasePrice,
                item.Quantity));
        }

        OnValidateReservation(lines, now);

        foreach (var line in lines)
        {
            GetTicketType(line.TicketTypeId).Reserve(line.Quantity);
        }

        return new TicketReservation(Id, lines);
    }

    /// <summary>Turns a hold into a sale once the booking has been paid for.</summary>
    public void ConfirmReservation(TicketReservation reservation)
    {
        EnsureOwns(reservation);

        foreach (var line in reservation.Lines)
        {
            GetTicketType(line.TicketTypeId).ConfirmReserved(line.Quantity);
        }
    }

    /// <summary>
    /// Returns seats to the pool, either from the held bucket (expired or abandoned checkout) or
    /// from the sold bucket (a cancelled booking).
    /// </summary>
    public void ReleaseReservation(TicketReservation reservation, bool wasPaidFor, DateTimeOffset now)
    {
        EnsureOwns(reservation);

        foreach (var line in reservation.Lines)
        {
            var ticketType = GetTicketType(line.TicketTypeId);
            if (wasPaidFor)
            {
                ticketType.ReleaseSold(line.Quantity);
            }
            else
            {
                ticketType.ReleaseReserved(line.Quantity);
            }
        }

        RaiseDomainEvent(new TicketsReleasedDomainEvent(Id, Title, reservation.TotalQuantity, now));
    }

    // --- waiting list ----------------------------------------------------------------------------------

    public WaitlistEntry JoinWaitlist(UserId customerId, int quantity, DateTimeOffset now)
    {
        Guard.Positive(quantity);

        if (Status != EventStatus.Published)
        {
            throw new InvalidStateTransitionException(Describe(), Status, "join the waiting list for");
        }

        if (AvailableTickets >= quantity)
        {
            throw new BusinessRuleViolationException(
                $"{Describe()} still has {AvailableTickets} ticket(s) available - book them instead.");
        }

        if (_waitlist.Any(entry => entry.CustomerId == customerId && entry.IsWaiting))
        {
            throw new BusinessRuleViolationException("You are already on the waiting list for this event.");
        }

        var entry = new WaitlistEntry(WaitlistEntryId.New(), customerId, quantity, now);
        _waitlist.Add(entry);
        return entry;
    }

    /// <summary>
    /// Picks the people at the front of the queue whose request fits into the seats that just came
    /// back, marks them as notified and returns them. First come, first served.
    /// </summary>
    public IReadOnlyList<WaitlistEntry> TakeWaitlistCandidates(int releasedQuantity, DateTimeOffset now)
    {
        Guard.Positive(releasedQuantity);

        var notified = new List<WaitlistEntry>();
        var remaining = releasedQuantity;

        foreach (var entry in _waitlist.Where(entry => entry.IsWaiting).OrderBy(entry => entry.JoinedAt))
        {
            if (entry.RequestedQuantity > remaining)
            {
                continue;
            }

            entry.MarkNotified(now);
            notified.Add(entry);
            remaining -= entry.RequestedQuantity;

            if (remaining == 0)
            {
                break;
            }
        }

        return notified;
    }

    // --- extension points ---------------------------------------------------------------------------------

    /// <summary>Extra conditions a subclass places on a single booking. Base implementation allows everything.</summary>
    protected virtual void OnValidateReservation(IReadOnlyList<ReservedTicketLine> lines, DateTimeOffset now)
    {
    }

    /// <summary>Extra conditions a subclass places on going on sale.</summary>
    protected virtual void OnValidatePublish()
    {
    }

    /// <summary>Periodic checks specific to a subclass; only called while the event is published and upcoming.</summary>
    protected virtual void OnScheduledMaintenance(DateTimeOffset now)
    {
    }

    protected string Describe() => $"{Category} '{Title}'";

    public override string ToString() => $"{Title} - {Schedule} @ {Venue}";

    /// <summary>Merges duplicate lines so that 2 + 3 VIP tickets is checked as one request for 5.</summary>
    private static List<TicketOrderItem> Consolidate(IReadOnlyCollection<TicketOrderItem> items)
    {
        foreach (var item in items)
        {
            if (item.Quantity <= 0)
            {
                throw new BusinessRuleViolationException("Every line of a booking must ask for at least one ticket.");
            }
        }

        return [.. items
            .GroupBy(item => item.TicketTypeId)
            .Select(group => new TicketOrderItem(group.Key, group.Sum(item => item.Quantity)))];
    }

    /// <summary>
    /// An event belongs to the organiser who created it. Every operation that changes it goes
    /// through here, so the rule cannot be left out of a new caller by accident.
    /// </summary>
    protected void EnsureManagedBy(UserId actingOrganizer)
    {
        if (actingOrganizer != OrganizerId)
        {
            throw new NotTheOrganizerException(Describe());
        }
    }

    private void EnsureNotFinished(string action)
    {
        if (Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            throw new InvalidStateTransitionException(Describe(), Status, action);
        }
    }

    private void EnsureOwns(TicketReservation reservation)
    {
        Guard.NotNull(reservation);

        if (reservation.EventId != Id)
        {
            throw new ArgumentException(
                $"Reservation belongs to event {reservation.EventId}, not to {Id}.",
                nameof(reservation));
        }
    }
}
