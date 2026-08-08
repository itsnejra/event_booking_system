using EventBooking.Domain.Common;
using EventBooking.Domain.Entities;
using EventBooking.Domain.Interfaces;
using EventBooking.Domain.Pricing;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Pricing.Rules;

/// <summary>Book far enough ahead and the ticket is cheaper. Rewards the certainty, not the customer.</summary>
public sealed class EarlyBirdDiscountRule : IPricingRule
{
    private readonly TimeSpan _leadTime;
    private readonly Percentage _rate;

    public EarlyBirdDiscountRule(TimeSpan leadTime, Percentage rate)
    {
        _leadTime = Guard.PositiveDuration(leadTime);
        _rate = rate;
    }

    public static EarlyBirdDiscountRule Default { get; } =
        new(TimeSpan.FromDays(30), Percentage.Of(15m));

    public string Name => $"Early bird ({_leadTime.TotalDays:0} days ahead)";

    public int Priority => 10;

    public bool AppliesTo(PricingContext context) =>
        context.Event.Schedule.NoticeBefore(context.Now) >= _leadTime;

    public Percentage Discount(PricingContext context) => _rate;
}
