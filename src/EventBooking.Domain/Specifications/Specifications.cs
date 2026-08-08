using EventBooking.Domain.Entities;

namespace EventBooking.Domain.Specifications;

/// <summary>
/// Entry points for building specifications. Non-generic companion to <see cref="Specification{T}"/>,
/// in the same way <c>Task</c> hosts the static helpers for <c>Task&lt;T&gt;</c>.
/// </summary>
public static class Specifications
{
    /// <summary>
    /// The neutral element. Useful as the seed when folding a list of optional filters:
    /// <c>filters.Aggregate(Specifications.AlwaysTrue&lt;Event&gt;(), (all, next) =&gt; all.And(next))</c>.
    /// </summary>
    public static Specification<T> AlwaysTrue<T>() => new AlwaysTrueSpecification<T>();
}
