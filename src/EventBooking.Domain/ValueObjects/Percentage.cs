using System.Globalization;
using EventBooking.Domain.Common;

namespace EventBooking.Domain.ValueObjects;

/// <summary>
/// A rate between 0 and 100. Discounts are expressed as percentages rather than as amounts so that
/// several of them can be reasoned about and capped together before any money is touched.
/// </summary>
public readonly record struct Percentage : IComparable<Percentage>, IFormattable
{
    public static readonly Percentage Zero = new(0m);
    public static readonly Percentage OneHundred = new(100m);

    private Percentage(decimal value) => Value = value;

    public decimal Value { get; }

    public decimal AsFraction => Value / 100m;

    public bool IsZero => Value == 0m;

    public static Percentage Of(decimal value)
    {
        if (value is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A percentage must be between 0 and 100.");
        }

        return new Percentage(decimal.Round(value, 4));
    }

    /// <summary>Adds two rates, saturating at 100% instead of overflowing into nonsense.</summary>
    public Percentage Add(Percentage other) => Of(Math.Min(100m, Value + other.Value));

    public Percentage Subtract(Percentage other) => Of(Math.Max(0m, Value - other.Value));

    /// <summary>The part of <paramref name="limit"/> still unused by this rate.</summary>
    public Percentage RemainingUpTo(Percentage limit) =>
        Value >= limit.Value ? Zero : Of(limit.Value - Value);

    public static Percentage operator +(Percentage left, Percentage right) => left.Add(right);

    public static Percentage operator -(Percentage left, Percentage right) => left.Subtract(right);

    public static bool operator <(Percentage left, Percentage right) => left.Value < right.Value;

    public static bool operator >(Percentage left, Percentage right) => left.Value > right.Value;

    public static bool operator <=(Percentage left, Percentage right) => left.Value <= right.Value;

    public static bool operator >=(Percentage left, Percentage right) => left.Value >= right.Value;

    public int CompareTo(Percentage other) => Value.CompareTo(other.Value);

    public override string ToString() => ToString(null, null);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format ?? "0.##", formatProvider ?? CultureInfo.InvariantCulture) + "%";
}
