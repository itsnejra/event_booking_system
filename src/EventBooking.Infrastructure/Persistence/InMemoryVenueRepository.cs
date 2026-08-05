using EventBooking.Domain.Abstractions;
using EventBooking.Domain.ValueObjects;
using EventBooking.Domain.Venues;

namespace EventBooking.Infrastructure.Persistence;

public sealed class InMemoryVenueRepository()
    : InMemoryRepository<Venue, VenueId>(venue => venue.Id), IVenueRepository
{
    public IReadOnlyCollection<Venue> GetByCity(string city) =>
        [.. Snapshot.Where(venue => string.Equals(venue.City, city, StringComparison.OrdinalIgnoreCase))];
}
