namespace EventBooking.Domain.Exceptions;

/// <summary>
/// Root of every error the domain raises on purpose. The distinction that matters:
/// a <see cref="DomainException"/> means the user asked for something the business does not allow,
/// so the message is safe to show them. An <see cref="ArgumentException"/> means the calling code
/// is wrong, and the user should never see it.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
