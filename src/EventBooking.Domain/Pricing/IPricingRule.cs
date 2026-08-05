using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Pricing;

/// <summary>
/// One reason a customer might pay less than the list price. Rules are independent objects, so a new
/// promotion is a new class registered with the engine - no existing rule and no service is touched.
/// </summary>
public interface IPricingRule
{
    /// <summary>Shown on the price breakdown, so it has to make sense to a customer.</summary>
    string Name { get; }

    /// <summary>Lower runs first. It matters because the total discount is capped.</summary>
    int Priority { get; }

    bool AppliesTo(PricingContext context);

    Percentage Discount(PricingContext context);
}
