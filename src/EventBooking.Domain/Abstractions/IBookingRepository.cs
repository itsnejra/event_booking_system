using EventBooking.Domain.Bookings;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Abstractions;

public interface IBookingRepository : IRepository<Booking, BookingId>
{
    Booking? FindByReference(BookingReference reference);

    IReadOnlyCollection<Booking> GetByCustomer(UserId customerId);

    IReadOnlyCollection<Booking> GetByEvent(EventId eventId);

    /// <summary>Holds whose clock has run out - the input for the expiry sweep.</summary>
    IReadOnlyCollection<Booking> GetExpiredHolds(DateTimeOffset asOf);
}
