using System.Runtime.CompilerServices;

namespace EventBooking.Domain.Common;

/// <summary>
/// Argument checks for constructors and factory methods.
/// These guard against <em>programmer</em> mistakes and therefore throw <see cref="ArgumentException"/>
/// and friends. Violations of business rules are a different thing entirely and throw
/// <see cref="Exceptions.DomainException"/> instead.
/// </summary>
public static class Guard
{
    public static T NotNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        return value;
    }

    /// <summary>Returns the trimmed text, so callers cannot accidentally store padded input.</summary>
    public static string NotEmpty(string? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty or whitespace.", parameterName);
        }

        return value.Trim();
    }

    public static string MaxLength(string value, int maxLength, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (value.Length > maxLength)
        {
            throw new ArgumentException($"Value must not exceed {maxLength} characters.", parameterName);
        }

        return value;
    }

    public static int Positive(int value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, parameterName);
        return value;
    }

    public static int NotNegative(int value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, parameterName);
        return value;
    }

    public static decimal NotNegative(decimal value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, parameterName);
        return value;
    }

    public static TimeSpan PositiveDuration(TimeSpan value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Duration must be greater than zero.");
        }

        return value;
    }
}
