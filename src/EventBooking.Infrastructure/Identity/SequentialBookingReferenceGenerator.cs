using EventBooking.Domain.Interfaces;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Infrastructure.Identity;

/// <summary>
/// Hands out BK-yyyy-00001 style codes from an in-process counter. A database sequence would replace
/// this class and nothing else.
/// </summary>
public sealed class SequentialBookingReferenceGenerator(IClock clock) : IBookingReferenceGenerator
{
    private int _sequence;

    public BookingReference NextReference() =>
        BookingReference.For(clock.UtcNow, Interlocked.Increment(ref _sequence));
}
