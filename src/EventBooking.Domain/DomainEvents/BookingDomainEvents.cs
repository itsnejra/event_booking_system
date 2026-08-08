using EventBooking.Domain.Common;
using EventBooking.Domain.Entities;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.DomainEvents;

// Facts published by the Booking aggregate.

public sealed record BookingPlacedDomainEvent(
    BookingId BookingId,
    BookingReference Reference,
    UserId CustomerId,
    EventId EventId,
    Money Total,
    DateTimeOffset HoldExpiresAt,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record BookingConfirmedDomainEvent(
    BookingId BookingId,
    BookingReference Reference,
    UserId CustomerId,
    EventId EventId,
    Money Total,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record BookingCancelledDomainEvent(
    BookingId BookingId,
    BookingReference Reference,
    UserId CustomerId,
    EventId EventId,
    Money RefundAmount,
    string Reason,
    bool WasPaidFor,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record BookingExpiredDomainEvent(
    BookingId BookingId,
    BookingReference Reference,
    UserId CustomerId,
    EventId EventId,
    DateTimeOffset OccurredAt) : IDomainEvent;
