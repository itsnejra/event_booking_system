namespace EventBooking.Domain.Abstractions;

/// <summary>
/// Reading the current time is an external dependency like any other. Half of this domain
/// (early bird pricing, refund windows, hold expiry, sales windows) is time dependent, and none of
/// it would be testable if the rules called <c>DateTimeOffset.UtcNow</c> directly.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
