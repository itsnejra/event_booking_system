
namespace EventBooking.Domain.Exceptions;

/// <summary>
/// Somebody tried to change an event they do not run.
/// </summary>
/// <remarks>
/// This lives in the domain rather than in a service because ownership is a business rule, not a
/// permission matrix: an event belongs to the organiser who created it, and that fact is part of
/// what an event <em>is</em>. Keeping the check on the aggregate means no caller can forget it.
/// </remarks>
public sealed class NotTheOrganizerException : DomainException
{
    public NotTheOrganizerException(string subject)
        : base($"Only the organiser who created {subject} can change it.")
    {
    }
}
