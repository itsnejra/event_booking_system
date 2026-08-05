using EventBooking.Domain.Exceptions;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Tests.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Constructor_RoundsToTwoDecimals()
    {
        var money = Money.Of(10.005m);

        Assert.Equal(10.01m, money.Amount);
    }

    [Fact]
    public void Constructor_RoundsHalfAwayFromZero()
    {
        Assert.Equal(0.13m, Money.Of(0.125m).Amount);
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Assert.Equal(Money.Of(25m), Money.Of(25m));
        Assert.NotEqual(Money.Of(25m), Money.Of(25m, Currency.EUR));
    }

    [Fact]
    public void Add_WithDifferentCurrencies_Throws()
    {
        Assert.Throws<CurrencyMismatchException>(() => Money.Of(10m, Currency.BAM) + Money.Of(10m, Currency.EUR));
    }

    [Fact]
    public void Compare_WithDifferentCurrencies_Throws()
    {
        Assert.Throws<CurrencyMismatchException>(() => Money.Of(10m, Currency.BAM) < Money.Of(10m, Currency.EUR));
    }

    [Fact]
    public void Multiply_ScalesByQuantity()
    {
        Assert.Equal(Money.Of(135m), Money.Of(45m) * 3);
    }

    [Theory]
    [InlineData(100, 15, 15)]
    [InlineData(45, 10, 4.5)]
    [InlineData(33.33, 50, 16.67)]
    public void Portion_TakesTheGivenRate(decimal amount, decimal rate, decimal expected)
    {
        Assert.Equal(Money.Of(expected), Money.Of(amount).Portion(Percentage.Of(rate)));
    }

    [Fact]
    public void SubtractOrZero_NeverGoesNegative()
    {
        Assert.Equal(Money.Zero(Currency.BAM), Money.Of(10m).SubtractOrZero(Money.Of(25m)));
    }

    [Fact]
    public void Sum_OfNothing_IsZeroInTheGivenCurrency()
    {
        var total = Money.Sum([], Currency.EUR);

        Assert.True(total.IsZero);
        Assert.Equal(Currency.EUR, total.Currency);
    }

    [Fact]
    public void ToString_ShowsAmountAndCurrency()
    {
        Assert.Equal("1,250.00 BAM", Money.Of(1250m).ToString());
    }
}
