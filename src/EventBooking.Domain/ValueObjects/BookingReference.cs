using System.Globalization;
using EventBooking.Domain.Common;

namespace EventBooking.Domain.ValueObjects;

/// <summary>
/// The code a customer quotes when they call about a booking - short, readable over the phone and
/// stable for the lifetime of the booking. Deliberately separate from <see cref="BookingId"/>:
/// one is for people, the other is for the system.
/// </summary>
public sealed record BookingReference
{
    private BookingReference(string value) => Value = value;

    public string Value { get; }

    public static BookingReference Create(string value)
    {
        var normalised = Guard.NotEmpty(value).ToUpperInvariant();
        return new BookingReference(Guard.MaxLength(normalised, 20));
    }

    public static BookingReference For(DateTimeOffset issuedAt, int sequenceNumber)
    {
        Guard.Positive(sequenceNumber);
        return new BookingReference(string.Format(
            CultureInfo.InvariantCulture,
            "BK-{0:yyyy}-{1:D5}",
            issuedAt,
            sequenceNumber));
    }

    public override string ToString() => Value;
}
