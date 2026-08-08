using EventBooking.Domain.Interfaces;

namespace EventBooking.Domain.Specifications;

/// <summary>
/// Base for composable specifications. Concrete rules only implement
/// <see cref="IsSatisfiedBy"/>; the boolean algebra is inherited once and for all.
/// </summary>
public abstract class Specification<T> : ISpecification<T>
{
    public abstract bool IsSatisfiedBy(T candidate);

    public Specification<T> And(Specification<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        // Folding a list of optional filters starts from All; without this the result would be a
        // pointlessly deep tree of "true AND (true AND ...)".
        if (this is AlwaysTrueSpecification<T>)
        {
            return other;
        }

        return other is AlwaysTrueSpecification<T> ? this : new AndSpecification<T>(this, other);
    }

    public Specification<T> Or(Specification<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new OrSpecification<T>(this, other);
    }

    public Specification<T> Not() => new NotSpecification<T>(this);

    public static Specification<T> operator &(Specification<T> left, Specification<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.And(right);
    }

    public static Specification<T> operator |(Specification<T> left, Specification<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.Or(right);
    }

    public static Specification<T> operator !(Specification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        return specification.Not();
    }
}
