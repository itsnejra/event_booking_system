
namespace EventBooking.Domain.Exceptions;

/// <summary>
/// Somebody tried to change an event they do not run.
/// </summary>
public sealed class NotTheOrganizerException : DomainException
{
    public NotTheOrganizerException(string subject)
        : base($"Only the organiser who created {subject} can change it.")
    {
    }
}
