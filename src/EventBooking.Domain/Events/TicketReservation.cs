using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Events;

/// <summary>A ticket type, a quantity and the list price the event committed to when it held the seats.</summary>
public sealed record ReservedTicketLine(
    TicketTypeId TicketTypeId,
    string TicketTypeName,
    TicketTier Tier,
    Money UnitPrice,
    int Quantity)
{
    public Money Subtotal => UnitPrice * Quantity;
}

/// <summary>
/// The event's answer to an order: these seats are now held for you, at these prices.
/// It carries no discounts - what the customer eventually pays is decided by the pricing engine,
/// which keeps inventory and money as two separate concerns.
/// </summary>
public sealed record TicketReservation(EventId EventId, IReadOnlyList<ReservedTicketLine> Lines)
{
    public int TotalQuantity => Lines.Sum(line => line.Quantity);

    public Money Subtotal => Money.Sum(Lines.Select(line => line.Subtotal), Lines[0].UnitPrice.Currency);
}
