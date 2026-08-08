using EventBooking.Domain.Enums;
using EventBooking.Domain.Interfaces;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Refunds;

/// <summary>Final sale. Kept as an explicit policy so "no refund" is a decision, not a missing branch.</summary>
public sealed class NoRefundPolicy : IRefundPolicy
{
    public static NoRefundPolicy Instance { get; } = new();

    private NoRefundPolicy()
    {
    }

    public string Description => "non-refundable";

    public Money CalculateRefund(Money amountPaid, DateTimeOffset eventStart, DateTimeOffset cancelledAt) =>
        Money.Zero(amountPaid.Currency);
}
