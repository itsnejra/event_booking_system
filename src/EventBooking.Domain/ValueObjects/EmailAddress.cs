using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace EventBooking.Domain.ValueObjects;

/// <summary>
/// A validated, normalised e-mail address. Once a <see cref="EmailAddress"/> exists it is known to
/// be well formed, which removes the "is this string actually an address?" question from every
/// method that takes one.
/// </summary>
public sealed partial record EmailAddress
{
    private const int MaxLength = 254;

    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public string Domain => Value[(Value.IndexOf('@', StringComparison.Ordinal) + 1)..];

    public static EmailAddress Create(string value)
    {
        if (!TryCreate(value, out var address))
        {
            throw new ArgumentException($"'{value}' is not a valid e-mail address.", nameof(value));
        }

        return address;
    }

    public static bool TryCreate(string? value, [NotNullWhen(true)] out EmailAddress? result)
    {
        var normalised = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalised.Length is 0 or > MaxLength || !Pattern().IsMatch(normalised))
        {
            result = null;
            return false;
        }

        result = new EmailAddress(normalised);
        return true;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$")]
    private static partial Regex Pattern();
}
