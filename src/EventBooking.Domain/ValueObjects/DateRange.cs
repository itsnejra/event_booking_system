using System.Globalization;

namespace EventBooking.Domain.ValueObjects;

/// <summary>
/// A half-open interval in time: <c>[Start, End)</c>. Modelled as a reference type rather than a
/// struct precisely because a default-constructed range would be meaningless - here the only way to
/// get one is through the constructor, which refuses to build an invalid interval.
/// </summary>
public sealed record DateRange
{
    public DateRange(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
        {
            throw new ArgumentException($"End ({end:u}) must be after start ({start:u}).", nameof(end));
        }

        Start = start;
        End = end;
    }

    public DateTimeOffset Start { get; }

    public DateTimeOffset End { get; }

    public TimeSpan Duration => End - Start;

    /// <summary>Number of calendar days the range touches, so a two day conference reads as 2.</summary>
    public int DayCount => (End.Date - Start.Date).Days + 1;

    public static DateRange Starting(DateTimeOffset start, TimeSpan duration) => new(start, start + duration);

    public bool Contains(DateTimeOffset moment) => moment >= Start && moment < End;

    public bool Overlaps(DateRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Start < other.End && other.Start < End;
    }

    /// <summary>True when the range is entirely in the future relative to <paramref name="moment"/>.</summary>
    public bool StartsAfter(DateTimeOffset moment) => Start > moment;

    public bool HasEnded(DateTimeOffset moment) => End <= moment;

    public TimeSpan NoticeBefore(DateTimeOffset moment) => Start - moment;

    public override string ToString() =>
        Start.Date == End.Date
            ? string.Format(CultureInfo.InvariantCulture, "{0:dd.MM.yyyy} {1:HH:mm}-{2:HH:mm}", Start, Start, End)
            : string.Format(CultureInfo.InvariantCulture, "{0:dd.MM.yyyy HH:mm} - {1:dd.MM.yyyy HH:mm}", Start, End);
}
