using EventBooking.Domain.Pricing;
using EventBooking.Domain.Pricing.Rules;
using EventBooking.Domain.Tests.TestKit;
using EventBooking.Domain.Users;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Tests.Pricing;

public sealed class PricingEngineTests
{
    private static readonly IPricingRule[] AllRules =
    [
        EarlyBirdDiscountRule.Default,
        GroupDiscountRule.Default,
        LoyaltyDiscountRule.Default,
    ];

    [Fact]
    public void WithNoRuleApplying_ThePriceIsTheListPrice()
    {
        var concert = Given.PublishedConcert(daysAhead: 5);
        var reservation = concert.Reserve(Given.Order(concert, "Parter", 1), Given.Now);

        var priced = new PricingEngine(AllRules).Price(concert, Given.Customer(), reservation, Given.Now);

        Assert.Equal(Money.Of(40m), priced.Total);
        Assert.Empty(priced.AppliedRuleNames);
    }

    [Fact]
    public void EarlyBird_AppliesWellBeforeTheEvent()
    {
        var concert = Given.PublishedConcert(daysAhead: 45);
        var reservation = concert.Reserve(Given.Order(concert, "Parter", 1), Given.Now);

        var priced = new PricingEngine(AllRules).Price(concert, Given.Customer(), reservation, Given.Now);

        Assert.Equal(Money.Of(34m), priced.Total);
    }

    [Fact]
    public void EarlyBird_StopsApplyingInsideTheWindow()
    {
        var concert = Given.PublishedConcert(daysAhead: 45);
        var reservation = concert.Reserve(Given.Order(concert, "Parter", 1), Given.Now);
        var justInsideTheWindow = concert.Schedule.Start.AddDays(-29);

        var priced = new PricingEngine(AllRules).Price(concert, Given.Customer(), reservation, justInsideTheWindow);

        Assert.Equal(Money.Of(40m), priced.Total);
    }

    [Fact]
    public void GroupDiscount_LooksAtTheWholeOrderRatherThanOneLine()
    {
        var conference = Given.PublishedConference();
        var reservation = conference.Reserve(Given.Order(conference, "Standard", 5), Given.Now);

        var priced = new PricingEngine([GroupDiscountRule.Default]).Price(
            conference,
            Given.Customer(),
            reservation,
            Given.Now);

        Assert.Equal(Money.Of(810m), priced.Total);
        Assert.Equal(Money.Of(90m), priced.DiscountTotal);
    }

    [Theory]
    [InlineData(MembershipTier.Standard, 40)]
    [InlineData(MembershipTier.Silver, 38)]
    [InlineData(MembershipTier.Gold, 36)]
    public void Loyalty_IsWorthMoreAtEachTier(MembershipTier tier, decimal expectedTotal)
    {
        var concert = Given.PublishedConcert(daysAhead: 5);
        var reservation = concert.Reserve(Given.Order(concert, "Parter", 1), Given.Now);

        var priced = new PricingEngine([LoyaltyDiscountRule.Default]).Price(
            concert,
            Given.Customer(tier),
            reservation,
            Given.Now);

        Assert.Equal(Money.Of(expectedTotal), priced.Total);
    }

    [Fact]
    public void Discounts_Stack()
    {
        var concert = Given.PublishedConcert(standardSeats: 100, daysAhead: 45);
        var reservation = concert.Reserve(Given.Order(concert, "Parter", 5), Given.Now);

        var priced = new PricingEngine(AllRules).Price(
            concert,
            Given.Customer(MembershipTier.Gold),
            reservation,
            Given.Now);

        // 15% early bird + 10% group + 10% loyalty = 35% off 200.00
        Assert.Equal(Money.Of(130m), priced.Total);
        Assert.Equal(3, priced.AppliedRuleNames.Count);
    }

    [Fact]
    public void StackedDiscounts_AreCappedAndTheLastOneIsTrimmedToFit()
    {
        var concert = Given.PublishedConcert(standardSeats: 100, daysAhead: 45);
        var reservation = concert.Reserve(Given.Order(concert, "Parter", 5), Given.Now);
        var cap = Percentage.Of(20m);

        var priced = new PricingEngine(AllRules, cap).Price(
            concert,
            Given.Customer(MembershipTier.Gold),
            reservation,
            Given.Now);

        Assert.Equal(Money.Of(160m), priced.Total);

        var granted = priced.Lines[0].Discounts;
        Assert.Equal(Percentage.Of(15m), granted[0].Rate);
        Assert.Equal(Percentage.Of(5m), granted[1].Rate);
        Assert.Equal(2, granted.Count);
    }

    [Fact]
    public void TheBreakdownExplainsEveryDiscountItGranted()
    {
        var concert = Given.PublishedConcert(daysAhead: 45);
        var reservation = concert.Reserve(Given.Order(concert, "Parter", 2), Given.Now);

        var priced = new PricingEngine(AllRules).Price(
            concert,
            Given.Customer(MembershipTier.Silver),
            reservation,
            Given.Now);

        var discounts = priced.Lines[0].Discounts;
        Assert.Equal(2, discounts.Count);
        Assert.Equal(Money.Of(12m), discounts[0].Amount);
        Assert.Equal(Money.Of(4m), discounts[1].Amount);
        Assert.Equal(priced.DiscountTotal, Money.Of(16m));
    }

    [Fact]
    public void RulesRunInPriorityOrderRegardlessOfHowTheyWereRegistered()
    {
        var concert = Given.PublishedConcert(standardSeats: 100, daysAhead: 45);
        var reservation = concert.Reserve(Given.Order(concert, "Parter", 5), Given.Now);

        var engine = new PricingEngine(
            [LoyaltyDiscountRule.Default, GroupDiscountRule.Default, EarlyBirdDiscountRule.Default]);

        var priced = engine.Price(concert, Given.Customer(MembershipTier.Gold), reservation, Given.Now);

        Assert.Equal(
            [EarlyBirdDiscountRule.Default.Name, GroupDiscountRule.Default.Name, LoyaltyDiscountRule.Default.Name],
            priced.Lines[0].Discounts.Select(discount => discount.RuleName));
    }

    [Fact]
    public void EachLineIsPricedOnItsOwn()
    {
        var concert = Given.PublishedConcert(standardSeats: 100, vipSeats: 20, daysAhead: 45);
        var reservation = concert.Reserve(
            [
                new Domain.Events.TicketOrderItem(Given.TicketTypeOf(concert, "Parter"), 4),
                new Domain.Events.TicketOrderItem(Given.TicketTypeOf(concert, "VIP"), 2),
            ],
            Given.Now);

        var priced = new PricingEngine(AllRules).Price(concert, Given.Customer(), reservation, Given.Now);

        // 6 tickets qualifies the whole order for the group discount: 25% off 160 and off 200.
        Assert.Equal(Money.Of(120m), priced.Lines[0].Total);
        Assert.Equal(Money.Of(150m), priced.Lines[1].Total);
        Assert.Equal(Money.Of(270m), priced.Total);
    }
}
