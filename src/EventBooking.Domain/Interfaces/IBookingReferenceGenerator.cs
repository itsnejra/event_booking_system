using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Interfaces;

/// <summary>
/// Hands out the next customer-facing booking code. Sequence numbers are an infrastructure concern
/// (a counter today, a database sequence tomorrow), so the domain only declares what it needs.
/// </summary>
public interface IBookingReferenceGenerator
{
    BookingReference NextReference();
}
