using EventBooking.Domain.Interfaces;

namespace EventBooking.Infrastructure.Time;

/// <summary>
/// The real clock plus an offset the demo can move.
/// </summary>
public sealed class AdjustableClock : IClock
{
    private readonly Lock _gate = new();
    private TimeSpan _offset = TimeSpan.Zero;

    public DateTimeOffset UtcNow
    {
        get
        {
            lock (_gate)
            {
                return DateTimeOffset.UtcNow + _offset;
            }
        }
    }

    public TimeSpan Offset
    {
        get
        {
            lock (_gate)
            {
                return _offset;
            }
        }
    }

    public void Advance(TimeSpan amount)
    {
        if (amount < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Time only moves forward here.");
        }

        lock (_gate)
        {
            _offset += amount;
        }
    }

    public void Reset()
    {
        lock (_gate)
        {
            _offset = TimeSpan.Zero;
        }
    }
}
