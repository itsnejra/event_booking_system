using EventBooking.Domain.Enums;

namespace EventBooking.Application.Reporting;

/// <summary>
/// Settings for cross-event reports. A single event reports in its own currency, but a total across
/// several of them needs one currency to be stated - anything else would be adding apples to pears.
/// </summary>
public sealed record ReportingOptions(Currency Currency)
{
    public static ReportingOptions Default { get; } = new(Currency.BAM);
}
