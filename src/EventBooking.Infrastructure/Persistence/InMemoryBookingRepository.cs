using EventBooking.Domain.Entities;
using EventBooking.Domain.Interfaces;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Infrastructure.Persistence;

public sealed class InMemoryBookingRepository()
    : InMemoryRepository<Booking, BookingId>(booking => booking.Id), IBookingRepository
{
    public Booking? FindByReference(BookingReference reference) =>
        Snapshot.FirstOrDefault(booking => booking.Reference == reference);

    public IReadOnlyCollection<Booking> GetByCustomer(UserId customerId) =>
        [.. Snapshot.Where(booking => booking.CustomerId == customerId)];

    public IReadOnlyCollection<Booking> GetByEvent(EventId eventId) =>
        [.. Snapshot.Where(booking => booking.EventId == eventId)];

    public IReadOnlyCollection<Booking> GetExpiredHolds(DateTimeOffset asOf) =>
        [.. Snapshot.Where(booking => booking.HasExpired(asOf))];
}
