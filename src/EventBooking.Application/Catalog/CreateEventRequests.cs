using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Catalog;

/// <summary>
/// What the outside world sends in to create an event. Requests are separate from the entities so
/// that a caller cannot construct a half-built aggregate and hand it over - the service is the only
/// thing that knows how to turn a request into a valid event.
/// </summary>
public abstract record CreateEventRequest
{
    public required string Title { get; init; }

    public required string Description { get; init; }

    public required DateRange Schedule { get; init; }

    public required VenueId VenueId { get; init; }

    public required UserId OrganizerId { get; init; }

    public Currency Currency { get; init; } = Currency.BAM;
}

public sealed record CreateConcertRequest : CreateEventRequest
{
    public required string Headliner { get; init; }
}

public sealed record CreateConferenceRequest : CreateEventRequest
{
    public required string Topic { get; init; }
}

public sealed record CreateWorkshopRequest : CreateEventRequest
{
    public required string Instructor { get; init; }

    public required int MinimumAttendees { get; init; }
}
