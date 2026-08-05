namespace EventBooking.Domain.Users;

/// <summary>
/// Loyalty level of a customer. The tier says nothing about discounts on purpose - what a tier is
/// worth is a pricing decision and lives in <c>Pricing/Rules/LoyaltyDiscountRule</c>, so that a
/// marketing change never touches the customer model.
/// </summary>
public enum MembershipTier
{
    Standard = 0,
    Silver = 1,
    Gold = 2,
}
