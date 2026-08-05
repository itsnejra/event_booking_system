using EventBooking.Domain.Tests.TestKit;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Tests.ValueObjects;

public sealed class DateRangeTests
{
    [Fact]
    public void Constructor_WithEndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DateRange(Given.Now, Given.Now.AddHours(-1)));
    }

    [Fact]
    public void Constructor_WithZeroLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DateRange(Given.Now, Given.Now));
    }

    [Fact]
    public void Contains_IsHalfOpen()
    {
        var range = DateRange.Starting(Given.Now, TimeSpan.FromHours(2));

        Assert.True(range.Contains(Given.Now));
        Assert.True(range.Contains(Given.Now.AddHours(1)));
        Assert.False(range.Contains(range.End));
    }

    [Fact]
    public void Overlaps_IsTrueWhenTheyShareTime()
    {
        var morning = DateRange.Starting(Given.Now, TimeSpan.FromHours(2));
        var overlapping = DateRange.Starting(Given.Now.AddHours(1), TimeSpan.FromHours(2));

        Assert.True(morning.Overlaps(overlapping));
        Assert.True(overlapping.Overlaps(morning));
    }

    [Fact]
    public void Overlaps_IsFalseWhenOneStartsExactlyWhereTheOtherEnds()
    {
        var first = DateRange.Starting(Given.Now, TimeSpan.FromHours(1));
        var second = DateRange.Starting(first.End, TimeSpan.FromHours(1));

        Assert.False(first.Overlaps(second));
    }

    [Fact]
    public void DayCount_CountsCalendarDaysTouched()
    {
        Assert.Equal(1, DateRange.Starting(Given.Now, TimeSpan.FromHours(3)).DayCount);
        Assert.Equal(2, DateRange.Starting(Given.Now, TimeSpan.FromDays(1)).DayCount);
    }

    [Fact]
    public void NoticeBefore_IsTheTimeLeftUntilTheStart()
    {
        var range = DateRange.Starting(Given.Now.AddDays(10), TimeSpan.FromHours(2));

        Assert.Equal(TimeSpan.FromDays(10), range.NoticeBefore(Given.Now));
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Assert.Equal(
            new DateRange(Given.Now, Given.Now.AddHours(2)),
            new DateRange(Given.Now, Given.Now.AddHours(2)));
    }
}
