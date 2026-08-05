using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Events;

/// <summary>One line of what a customer is asking for, before the event has agreed to it.</summary>
public sealed record TicketOrderItem(TicketTypeId TicketTypeId, int Quantity);
