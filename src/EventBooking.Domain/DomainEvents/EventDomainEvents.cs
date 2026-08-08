using EventBooking.Domain.Common;
using EventBooking.Domain.Entities;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.DomainEvents;

// Facts published by the Event aggregate.

public sealed record EventPublishedDomainEvent(EventId EventId, string Title, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record EventCancelledDomainEvent(
    EventId EventId,
    string Title,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record EventRescheduledDomainEvent(
    EventId EventId,
    string Title,
    DateRange PreviousSchedule,
    DateRange NewSchedule,
    DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Seats went back into the pool - the waiting list cares about this one.</summary>
public sealed record TicketsReleasedDomainEvent(
    EventId EventId,
    string Title,
    int Quantity,
    DateTimeOffset OccurredAt) : IDomainEvent;
