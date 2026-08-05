using EventBooking.Domain.Abstractions;
using EventBooking.Domain.Events;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Infrastructure.Persistence;

public sealed class InMemoryEventRepository()
    : InMemoryRepository<Event, EventId>(@event => @event.Id), IEventRepository
{
    public IReadOnlyCollection<Event> Find(ISpecification<Event> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        return [.. Snapshot.Where(specification.IsSatisfiedBy)];
    }

    public IReadOnlyCollection<Event> GetByOrganizer(UserId organizerId) =>
        [.. Snapshot.Where(@event => @event.OrganizerId == organizerId)];
}
