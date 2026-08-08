
namespace EventBooking.Domain.ValueObjects;

// Identifiers get their own types rather than being raw Guids.

public readonly record struct EventId(Guid Value)
{
    public static EventId New() => new(Guid.NewGuid());

    /// <summary>Short, human readable form used in console output and log messages.</summary>
    public string ShortCode => Value.ToString("N")[..6].ToUpperInvariant();

    public override string ToString() => Value.ToString("D");
}

public readonly record struct TicketTypeId(Guid Value)
{
    public static TicketTypeId New() => new(Guid.NewGuid());

    public string ShortCode => Value.ToString("N")[..6].ToUpperInvariant();

    public override string ToString() => Value.ToString("D");
}

public readonly record struct BookingId(Guid Value)
{
    public static BookingId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}

public readonly record struct VenueId(Guid Value)
{
    public static VenueId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}

public readonly record struct WaitlistEntryId(Guid Value)
{
    public static WaitlistEntryId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}
