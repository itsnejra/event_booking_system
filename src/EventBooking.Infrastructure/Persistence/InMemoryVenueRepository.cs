using EventBooking.Domain.Entities;
using EventBooking.Domain.Interfaces;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Infrastructure.Persistence;

public sealed class InMemoryVenueRepository()
    : InMemoryRepository<Venue, VenueId>(venue => venue.Id), IVenueRepository
{
    public IReadOnlyCollection<Venue> GetByCity(string city) =>
        [.. Snapshot.Where(venue => string.Equals(venue.City, city, StringComparison.OrdinalIgnoreCase))];
}
