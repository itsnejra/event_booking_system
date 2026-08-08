
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Interfaces;

/// <summary>
/// How much money a customer gets back when they cancel. Different kinds of event answer this
/// question very differently, so the answer is a strategy the event chooses rather than a chain of
/// <c>if (event is Concert)</c> checks in a service.
/// </summary>
public interface IRefundPolicy
{
    /// <summary>Human readable summary, shown to the customer before they confirm a cancellation.</summary>
    string Description { get; }

    Money CalculateRefund(Money amountPaid, DateTimeOffset eventStart, DateTimeOffset cancelledAt);
}
