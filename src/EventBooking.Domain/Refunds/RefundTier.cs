using System.Globalization;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Refunds;

/// <summary>One step of a <see cref="TieredRefundPolicy"/>: cancel this early, get this much back.</summary>
public sealed record RefundTier(TimeSpan MinimumNotice, Percentage Rate)
{
    public string Describe() =>
        string.Format(CultureInfo.InvariantCulture, "{0} when cancelled at least {1} in advance", Rate, DescribeNotice());

    private string DescribeNotice() => MinimumNotice.TotalDays >= 1
        ? string.Format(CultureInfo.InvariantCulture, "{0:0.#} day(s)", MinimumNotice.TotalDays)
        : string.Format(CultureInfo.InvariantCulture, "{0:0} hour(s)", MinimumNotice.TotalHours);
}
