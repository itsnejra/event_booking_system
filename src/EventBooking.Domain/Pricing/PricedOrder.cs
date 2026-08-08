using EventBooking.Domain.Enums;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Pricing;

/// <summary>A single discount that was actually granted, with the money it saved.</summary>
public sealed record AppliedDiscount(string RuleName, Percentage Rate, Money Amount)
{
    public override string ToString() => $"{RuleName} (-{Rate}) = -{Amount}";
}

/// <summary>One reserved line together with the discounts that ended up applying to it.</summary>
public sealed record PricedLine(ReservedTicketLine Ticket, IReadOnlyList<AppliedDiscount> Discounts)
{
    public Money Subtotal => Ticket.Subtotal;

    public Money DiscountTotal => Money.Sum(Discounts.Select(discount => discount.Amount), Subtotal.Currency);

    public Money Total => Subtotal.SubtractOrZero(DiscountTotal);
}

/// <summary>
/// What the customer is asked to pay, and why. The breakdown is kept rather than collapsed into a
/// single number so that the console, an invoice or a support agent can all explain the price.
/// </summary>
public sealed record PricedOrder(EventId EventId, IReadOnlyList<PricedLine> Lines)
{
    public Currency Currency => Lines[0].Subtotal.Currency;

    public int TotalTickets => Lines.Sum(line => line.Ticket.Quantity);

    public Money Subtotal => Money.Sum(Lines.Select(line => line.Subtotal), Currency);

    public Money DiscountTotal => Money.Sum(Lines.Select(line => line.DiscountTotal), Currency);

    public Money Total => Money.Sum(Lines.Select(line => line.Total), Currency);

    public IReadOnlyCollection<string> AppliedRuleNames =>
        [.. Lines.SelectMany(line => line.Discounts).Select(discount => discount.RuleName).Distinct(StringComparer.Ordinal)];
}
