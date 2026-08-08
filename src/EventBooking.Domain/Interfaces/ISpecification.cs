
namespace EventBooking.Domain.Interfaces;

/// <summary>
/// A single, named business predicate. Filters are modelled as objects so that they can be named,
/// unit tested on their own and combined, instead of being copy-pasted as anonymous lambdas across
/// services and menus.
/// </summary>
public interface ISpecification<in T>
{
    bool IsSatisfiedBy(T candidate);
}
