using System.Globalization;
using EventBooking.Domain.Exceptions;

namespace EventBooking.Domain.ValueObjects;

/// <summary>
/// An amount together with its currency. Rounding happens once, in the constructor, so that a total
/// can never drift away from the sum of the lines that produced it, and arithmetic across
/// currencies fails loudly instead of silently producing a wrong number.
/// </summary>
public readonly record struct Money : IComparable<Money>, IFormattable
{
    public const int DecimalPlaces = 2;

    public Money(decimal amount, Currency currency)
    {
        Amount = decimal.Round(amount, DecimalPlaces, MidpointRounding.AwayFromZero);
        Currency = currency;
    }

    public decimal Amount { get; }

    public Currency Currency { get; }

    public bool IsZero => Amount == 0m;

    public bool IsPositive => Amount > 0m;

    public static Money Zero(Currency currency) => new(0m, currency);

    public static Money Of(decimal amount, Currency currency = Currency.BAM) => new(amount, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(this, other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(this, other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(int factor) => new(Amount * factor, Currency);

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    /// <summary>The slice of this amount described by <paramref name="rate"/>, e.g. 15% of 40 BAM.</summary>
    public Money Portion(Percentage rate) => new(Amount * rate.AsFraction, Currency);

    /// <summary>Clamps at zero, because a negative price is never the right answer here.</summary>
    public Money SubtractOrZero(Money other)
    {
        EnsureSameCurrency(this, other);
        return new Money(Math.Max(0m, Amount - other.Amount), Currency);
    }

    public static Money Sum(IEnumerable<Money> amounts, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(amounts);

        var total = Zero(currency);
        foreach (var amount in amounts)
        {
            total = total.Add(amount);
        }

        return total;
    }

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static Money operator *(Money money, int factor) => money.Multiply(factor);

    public static Money operator *(Money money, decimal factor) => money.Multiply(factor);

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;

    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;

    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    public int CompareTo(Money other)
    {
        EnsureSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }

    public override string ToString() => ToString(null, null);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        $"{Amount.ToString(format ?? "N2", formatProvider ?? CultureInfo.InvariantCulture)} {Currency}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new CurrencyMismatchException(left.Currency, right.Currency);
        }
    }
}
