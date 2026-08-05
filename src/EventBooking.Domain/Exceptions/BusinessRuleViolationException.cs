namespace EventBooking.Domain.Exceptions;

/// <summary>The requested operation would break a rule of the business.</summary>
public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message)
        : base(message)
    {
    }
}
