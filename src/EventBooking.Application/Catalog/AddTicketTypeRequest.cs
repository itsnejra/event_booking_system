using EventBooking.Domain.Events;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Catalog;

public sealed record AddTicketTypeRequest
{
    public required string Name { get; init; }

    public required TicketTier Tier { get; init; }

    public required Money Price { get; init; }

    public required int Capacity { get; init; }

    /// <summary>When set, the ticket is only sold inside this window - how a limited batch is modelled.</summary>
    public DateRange? SalesWindow { get; init; }
}
