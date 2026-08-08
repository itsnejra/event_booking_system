using EventBooking.Domain.Common;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Entities;

/// <summary>
/// A physical place with a hard capacity. Venues are shared between events, which is why the
/// capacity check lives on the event: the venue states a fact, the event respects it.
/// </summary>
public sealed class Venue : Entity<VenueId>
{
    public Venue(VenueId id, string name, string city, int capacity)
        : base(id)
    {
        Name = Guard.MaxLength(Guard.NotEmpty(name), 120);
        City = Guard.MaxLength(Guard.NotEmpty(city), 80);
        Capacity = Guard.Positive(capacity);
    }

    public string Name { get; }

    public string City { get; }

    public int Capacity { get; }

    public override string ToString() => $"{Name}, {City}";
}
