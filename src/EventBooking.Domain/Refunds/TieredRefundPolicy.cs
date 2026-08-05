using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Refunds;

/// <summary>
/// The usual shape of a real refund rule: the earlier you cancel, the more you get back.
/// Tiers are sorted on construction, so callers can list them in whatever order reads best.
/// </summary>
public sealed class TieredRefundPolicy : IRefundPolicy
{
    private readonly IReadOnlyList<RefundTier> _tiers;

    public TieredRefundPolicy(params RefundTier[] tiers)
    {
        ArgumentNullException.ThrowIfNull(tiers);
        if (tiers.Length == 0)
        {
            throw new ArgumentException("A tiered policy needs at least one tier.", nameof(tiers));
        }

        _tiers = [.. tiers.OrderByDescending(tier => tier.MinimumNotice)];
    }

    public string Description => string.Join("; ", _tiers.Select(tier => tier.Describe())) + "; nothing afterwards";

    public Money CalculateRefund(Money amountPaid, DateTimeOffset eventStart, DateTimeOffset cancelledAt)
    {
        var notice = eventStart - cancelledAt;
        var applicable = _tiers.FirstOrDefault(tier => notice >= tier.MinimumNotice);

        return applicable is null
            ? Money.Zero(amountPaid.Currency)
            : amountPaid.Portion(applicable.Rate);
    }
}
