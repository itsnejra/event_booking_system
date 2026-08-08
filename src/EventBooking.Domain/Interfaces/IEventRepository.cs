using EventBooking.Domain.Entities;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Interfaces;

public interface IEventRepository : IRepository<Event, EventId>
{
    /// <summary>
    /// Everything matching the given rule. Taking a specification rather than a predicate keeps the
    /// filter a first class object that the caller can name, reuse and test.
    /// </summary>
    IReadOnlyCollection<Event> Find(ISpecification<Event> specification);

    IReadOnlyCollection<Event> GetByOrganizer(UserId organizerId);
}
