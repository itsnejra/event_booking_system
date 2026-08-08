using EventBooking.Domain.Entities;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Pricing;

/// <summary>
/// Everything a pricing rule is allowed to look at. Passing one object instead of five parameters
/// means a new rule that needs a new fact does not change the signature every other rule implements.
/// </summary>
public sealed record PricingContext(
    Event Event,
    Customer Customer,
    ReservedTicketLine Line,
    int TotalTicketsInOrder,
    DateTimeOffset Now);
