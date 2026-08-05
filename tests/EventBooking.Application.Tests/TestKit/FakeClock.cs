using EventBooking.Domain.Abstractions;

namespace EventBooking.Application.Tests.TestKit;

/// <summary>A clock the test drives, so time-dependent behaviour is asserted rather than waited for.</summary>
internal sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = now;

    public void Advance(TimeSpan amount) => UtcNow += amount;

    public void MoveTo(DateTimeOffset moment) => UtcNow = moment;
}
