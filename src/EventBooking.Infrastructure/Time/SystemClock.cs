using EventBooking.Domain.Abstractions;

namespace EventBooking.Infrastructure.Time;

/// <summary>The real clock. The one implementation that belongs in production.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
