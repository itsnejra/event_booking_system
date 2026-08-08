using EventBooking.Domain.Entities;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Interfaces;

public interface IBookingRepository : IRepository<Booking, BookingId>
{
    Booking? FindByReference(BookingReference reference);

    IReadOnlyCollection<Booking> GetByCustomer(UserId customerId);

    IReadOnlyCollection<Booking> GetByEvent(EventId eventId);

    /// <summary>Holds whose clock has run out - the input for the expiry sweep.</summary>
    IReadOnlyCollection<Booking> GetExpiredHolds(DateTimeOffset asOf);
}
