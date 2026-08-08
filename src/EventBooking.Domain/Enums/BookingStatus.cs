
namespace EventBooking.Domain.Enums;

/// <summary>
/// Pending -> Confirmed -> Cancelled, with Expired as the automatic exit for a hold nobody paid for.
/// Refunded is not a status: a refund is an amount attached to a cancellation, and modelling it as
/// a state would make "cancelled with no refund due" impossible to express.
/// </summary>
public enum BookingStatus
{
    /// <summary>Seats are held, payment is outstanding.</summary>
    Pending = 0,

    Confirmed = 1,

    Cancelled = 2,

    /// <summary>The hold ran out before it was paid for.</summary>
    Expired = 3,
}
