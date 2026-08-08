using EventBooking.Domain.Entities;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Interfaces;

public interface IVenueRepository : IRepository<Venue, VenueId>
{
    IReadOnlyCollection<Venue> GetByCity(string city);
}
