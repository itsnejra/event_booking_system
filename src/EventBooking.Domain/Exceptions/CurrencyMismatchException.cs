
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Exceptions;

/// <summary>
/// Two monetary amounts in different currencies were combined. This is a bug rather than a user
/// error, but it lives here because <c>Money</c> is a domain concept and the message is useful.
/// </summary>
public sealed class CurrencyMismatchException : DomainException
{
    public CurrencyMismatchException(object left, object right)
        : base($"Cannot combine amounts in {left} and {right}.")
    {
    }
}
