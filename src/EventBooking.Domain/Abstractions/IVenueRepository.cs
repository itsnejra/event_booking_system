using EventBooking.Domain.ValueObjects;
using EventBooking.Domain.Venues;

namespace EventBooking.Domain.Abstractions;

public interface IVenueRepository : IRepository<Venue, VenueId>
{
    IReadOnlyCollection<Venue> GetByCity(string city);
}
