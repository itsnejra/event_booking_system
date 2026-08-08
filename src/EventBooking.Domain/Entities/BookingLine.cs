using EventBooking.Domain.Enums;
using EventBooking.Domain.Pricing;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Entities;

/// <summary>
/// One line of a booking, frozen at the moment it was placed. The ticket type name and price are
/// copied rather than looked up, because a receipt has to keep saying what the customer agreed to
/// even after the organiser renames the ticket or changes its price.
/// </summary>
public sealed record BookingLine(
    TicketTypeId TicketTypeId,
    string TicketTypeName,
    TicketTier Tier,
    int Quantity,
    Money UnitPrice,
    IReadOnlyList<AppliedDiscount> AppliedDiscounts)
{
    public Money Subtotal => UnitPrice * Quantity;

    /// <summary>
    /// Derived from the individual discounts rather than stored alongside them, so the total on a
    /// receipt can never disagree with the reasons printed underneath it.
    /// </summary>
    public Money Discount => Money.Sum(AppliedDiscounts.Select(applied => applied.Amount), UnitPrice.Currency);

    public Money Total => Subtotal.SubtractOrZero(Discount);

    public static BookingLine From(PricedLine pricedLine)
    {
        ArgumentNullException.ThrowIfNull(pricedLine);

        return new BookingLine(
            pricedLine.Ticket.TicketTypeId,
            pricedLine.Ticket.TicketTypeName,
            pricedLine.Ticket.Tier,
            pricedLine.Ticket.Quantity,
            pricedLine.Ticket.UnitPrice,
            pricedLine.Discounts);
    }

    public override string ToString() => $"{Quantity} x {TicketTypeName} @ {UnitPrice} = {Total}";
}
