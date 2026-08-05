using EventBooking.Domain.Users;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Pricing.Rules;

/// <summary>
/// Rewards repeat customers. This is the single place that decides what a membership tier is
/// actually worth - <see cref="MembershipTier"/> itself stays a plain label.
/// </summary>
public sealed class LoyaltyDiscountRule : IPricingRule
{
    private readonly IReadOnlyDictionary<MembershipTier, Percentage> _rates;

    public LoyaltyDiscountRule(IReadOnlyDictionary<MembershipTier, Percentage> rates)
    {
        ArgumentNullException.ThrowIfNull(rates);
        _rates = rates;
    }

    public static LoyaltyDiscountRule Default { get; } = new(new Dictionary<MembershipTier, Percentage>
    {
        [MembershipTier.Silver] = Percentage.Of(5m),
        [MembershipTier.Gold] = Percentage.Of(10m),
    });

    public string Name => "Loyalty";

    public int Priority => 30;

    public bool AppliesTo(PricingContext context) => _rates.ContainsKey(context.Customer.Tier);

    public Percentage Discount(PricingContext context) =>
        _rates.TryGetValue(context.Customer.Tier, out var rate) ? rate : Percentage.Zero;
}
