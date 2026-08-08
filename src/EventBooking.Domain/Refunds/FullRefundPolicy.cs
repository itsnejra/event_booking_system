using EventBooking.Domain.Interfaces;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Refunds;

/// <summary>
/// Everything back, no questions asked. Used when the organiser is the one who cancels - the
/// customer should never be out of pocket for somebody else's decision.
/// </summary>
public sealed class FullRefundPolicy : IRefundPolicy
{
    public static FullRefundPolicy Instance { get; } = new();

    private FullRefundPolicy()
    {
    }

    public string Description => "100% refund";

    public Money CalculateRefund(Money amountPaid, DateTimeOffset eventStart, DateTimeOffset cancelledAt) => amountPaid;
}
