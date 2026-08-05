using EventBooking.Domain.Common;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Pricing.Rules;

/// <summary>
/// Bulk discount. It looks at the whole order rather than the line, so five tickets spread over two
/// ticket types still count as a group of five.
/// </summary>
public sealed class GroupDiscountRule : IPricingRule
{
    private readonly int _minimumTickets;
    private readonly Percentage _rate;

    public GroupDiscountRule(int minimumTickets, Percentage rate)
    {
        _minimumTickets = Guard.Positive(minimumTickets);
        _rate = rate;
    }

    public static GroupDiscountRule Default { get; } = new(5, Percentage.Of(10m));

    public string Name => $"Group booking ({_minimumTickets}+ tickets)";

    public int Priority => 20;

    public bool AppliesTo(PricingContext context) => context.TotalTicketsInOrder >= _minimumTickets;

    public Percentage Discount(PricingContext context) => _rate;
}
