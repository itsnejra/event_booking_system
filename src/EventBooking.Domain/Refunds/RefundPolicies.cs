using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Refunds;

/// <summary>Named presets, so event types can express their policy in one readable line.</summary>
public static class RefundPolicies
{
    /// <summary>Full refund up to <paramref name="notice"/> before the start, nothing after that.</summary>
    public static IRefundPolicy FullUntil(TimeSpan notice) =>
        new TieredRefundPolicy(new RefundTier(notice, Percentage.OneHundred));

    /// <summary>Full refund two weeks out, half up to three days out, nothing after that.</summary>
    public static IRefundPolicy Graduated() => new TieredRefundPolicy(
        new RefundTier(TimeSpan.FromDays(14), Percentage.OneHundred),
        new RefundTier(TimeSpan.FromDays(3), Percentage.Of(50m)));
}
