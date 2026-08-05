using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Tests.ValueObjects;

public sealed class PercentageTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Of_OutsideZeroToHundred_Throws(decimal value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Percentage.Of(value));
    }

    [Fact]
    public void AsFraction_ConvertsToAMultiplier()
    {
        Assert.Equal(0.15m, Percentage.Of(15m).AsFraction);
    }

    [Fact]
    public void Add_SaturatesAtOneHundred()
    {
        Assert.Equal(Percentage.OneHundred, Percentage.Of(80m).Add(Percentage.Of(50m)));
    }

    [Fact]
    public void RemainingUpTo_ReportsTheUnusedHeadroom()
    {
        Assert.Equal(Percentage.Of(20m), Percentage.Of(15m).RemainingUpTo(Percentage.Of(35m)));
    }

    [Fact]
    public void RemainingUpTo_WhenLimitIsAlreadyReached_IsZero()
    {
        Assert.True(Percentage.Of(35m).RemainingUpTo(Percentage.Of(35m)).IsZero);
    }

    [Fact]
    public void Default_IsZero()
    {
        Assert.True(default(Percentage).IsZero);
    }
}
