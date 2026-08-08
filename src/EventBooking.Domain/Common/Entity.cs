using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Common;

/// <summary>
/// Base class for objects with a lifetime and an identity. Two entities are the same entity when
/// they are of the same type and carry the same identifier - their attributes may well differ,
/// for example because one of them was loaded before an update.
/// </summary>
/// <typeparam name="TId">A strongly typed identifier (see <c>ValueObjects/Identifiers.cs</c>).</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct
{
    protected Entity(TId id) => Id = id;

    public TId Id { get; }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetType() == other.GetType() && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
