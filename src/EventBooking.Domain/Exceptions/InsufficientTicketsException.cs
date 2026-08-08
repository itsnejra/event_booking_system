
namespace EventBooking.Domain.Exceptions;

/// <summary>
/// Not enough tickets left to satisfy the request. Carries the numbers so the UI can offer the
/// waiting list without having to re-query the event.
/// </summary>
public sealed class InsufficientTicketsException : DomainException
{
    public InsufficientTicketsException(string ticketTypeName, int requested, int available)
        : base($"Only {available} '{ticketTypeName}' ticket(s) left, {requested} requested.")
    {
        TicketTypeName = ticketTypeName;
        Requested = requested;
        Available = available;
    }

    public string TicketTypeName { get; }

    public int Requested { get; }

    public int Available { get; }
}
