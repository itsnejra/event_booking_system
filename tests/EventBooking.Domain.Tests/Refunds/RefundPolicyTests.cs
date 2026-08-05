using EventBooking.Domain.Refunds;
using EventBooking.Domain.Tests.TestKit;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Tests.Refunds;

public sealed class RefundPolicyTests
{
    private static readonly Money Paid = Money.Of(100m);

    [Theory]
    [InlineData(20, 100)]
    [InlineData(14, 100)]
    [InlineData(10, 50)]
    [InlineData(3, 50)]
    [InlineData(1, 0)]
    [InlineData(0, 0)]
    public void Graduated_PaysLessTheLaterYouCancel(double daysOfNotice, decimal expectedRefund)
    {
        var eventStart = Given.Now.AddDays(daysOfNotice);

        var refund = RefundPolicies.Graduated().CalculateRefund(Paid, eventStart, Given.Now);

        Assert.Equal(Money.Of(expectedRefund), refund);
    }

    [Fact]
    public void Graduated_AfterTheEventHasStarted_PaysNothing()
    {
        var refund = RefundPolicies.Graduated().CalculateRefund(Paid, Given.Now.AddDays(-1), Given.Now);

        Assert.True(refund.IsZero);
    }

    [Theory]
    [InlineData(8, 100)]
    [InlineData(7, 100)]
    [InlineData(6, 0)]
    public void FullUntil_IsAllOrNothingAtTheCutoff(double daysOfNotice, decimal expectedRefund)
    {
        var policy = RefundPolicies.FullUntil(TimeSpan.FromDays(7));

        var refund = policy.CalculateRefund(Paid, Given.Now.AddDays(daysOfNotice), Given.Now);

        Assert.Equal(Money.Of(expectedRefund), refund);
    }

    [Fact]
    public void FullRefundPolicy_AlwaysPaysEverythingBack()
    {
        var refund = FullRefundPolicy.Instance.CalculateRefund(Paid, Given.Now.AddMinutes(1), Given.Now);

        Assert.Equal(Paid, refund);
    }

    [Fact]
    public void NoRefundPolicy_NeverPaysAnythingBack()
    {
        var refund = NoRefundPolicy.Instance.CalculateRefund(Paid, Given.Now.AddYears(1), Given.Now);

        Assert.True(refund.IsZero);
    }

    [Fact]
    public void RefundKeepsTheCurrencyItWasPaidIn()
    {
        var refund = RefundPolicies.Graduated()
            .CalculateRefund(Money.Of(80m, Currency.EUR), Given.Now.AddDays(30), Given.Now);

        Assert.Equal(Currency.EUR, refund.Currency);
    }

    [Fact]
    public void TieredPolicy_SortsItsTiersRegardlessOfTheOrderTheyWereGivenIn()
    {
        var policy = new TieredRefundPolicy(
            new RefundTier(TimeSpan.FromDays(3), Percentage.Of(50m)),
            new RefundTier(TimeSpan.FromDays(14), Percentage.OneHundred));

        Assert.Equal(Money.Of(100m), policy.CalculateRefund(Paid, Given.Now.AddDays(20), Given.Now));
    }

    [Fact]
    public void TieredPolicy_WithoutTiers_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TieredRefundPolicy());
    }

    [Fact]
    public void EachEventTypeCarriesItsOwnPolicy()
    {
        var eightDaysOfNotice = Given.Now.AddDays(8);

        // A concert is already down to half at eight days; a conference is still fully refundable.
        Assert.Equal(Money.Of(50m), Given.Concert().RefundPolicy.CalculateRefund(Paid, eightDaysOfNotice, Given.Now));
        Assert.Equal(Money.Of(100m), Given.Conference().RefundPolicy.CalculateRefund(Paid, eightDaysOfNotice, Given.Now));
        Assert.Equal(Money.Of(100m), Given.Workshop().RefundPolicy.CalculateRefund(Paid, eightDaysOfNotice, Given.Now));
    }
}
