
namespace EventBooking.Domain.Enums;

/// <summary>
/// What a ticket entitles you to. Early bird is not a tier - it is a price you get for booking
/// early, and it is handled by a pricing rule rather than by a separate kind of ticket.
/// </summary>
public enum TicketTier
{
    Standard = 0,
    Vip = 1,
    Student = 2,
}
