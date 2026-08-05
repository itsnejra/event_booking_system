using EventBooking.Domain.Abstractions;

namespace EventBooking.Domain.Tests.TestKit;

/// <summary>
/// A clock the test drives. This is the reason <see cref="IClock"/> exists: rules about early bird
/// windows, refund tiers and expiring holds are checked in microseconds instead of in weeks.
/// </summary>
internal sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = now;

    public void Advance(TimeSpan amount) => UtcNow += amount;
}
