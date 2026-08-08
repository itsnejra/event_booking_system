
namespace EventBooking.Domain.Specifications;

// The combinators behind Specification<T>.And / .Or / .Not. They are internal because callers should
// build them through those methods rather than newing them up directly.

internal sealed class AndSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override bool IsSatisfiedBy(T candidate) =>
        left.IsSatisfiedBy(candidate) && right.IsSatisfiedBy(candidate);
}

internal sealed class OrSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override bool IsSatisfiedBy(T candidate) =>
        left.IsSatisfiedBy(candidate) || right.IsSatisfiedBy(candidate);
}

internal sealed class NotSpecification<T>(Specification<T> inner) : Specification<T>
{
    public override bool IsSatisfiedBy(T candidate) => !inner.IsSatisfiedBy(candidate);
}

internal sealed class AlwaysTrueSpecification<T> : Specification<T>
{
    public override bool IsSatisfiedBy(T candidate) => true;
}
