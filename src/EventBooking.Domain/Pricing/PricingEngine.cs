using EventBooking.Domain.Common;
using EventBooking.Domain.Entities;
using EventBooking.Domain.Interfaces;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Pricing;

/// <summary>
/// Turns a reservation into a price by running every registered rule over every line.
/// </summary>
public sealed class PricingEngine
{
    public static readonly Percentage DefaultMaximumDiscount = Percentage.Of(35m);

    private readonly List<IPricingRule> _rules;
    private readonly Percentage _maximumDiscount;

    public PricingEngine(IEnumerable<IPricingRule> rules)
        : this(rules, DefaultMaximumDiscount)
    {
    }

    public PricingEngine(IEnumerable<IPricingRule> rules, Percentage maximumDiscount)
    {
        Guard.NotNull(rules);

        // Ordering by name as well keeps the outcome deterministic when two rules share a priority.
        _rules = [.. rules.OrderBy(rule => rule.Priority).ThenBy(rule => rule.Name, StringComparer.Ordinal)];
        _maximumDiscount = maximumDiscount;
    }

    public IReadOnlyList<IPricingRule> Rules => _rules.AsReadOnly();

    public Percentage MaximumDiscount => _maximumDiscount;

    public PricedOrder Price(Event @event, Customer customer, TicketReservation reservation, DateTimeOffset now)
    {
        Guard.NotNull(@event);
        Guard.NotNull(customer);
        Guard.NotNull(reservation);

        var pricedLines = new List<PricedLine>(reservation.Lines.Count);

        foreach (var line in reservation.Lines)
        {
            var context = new PricingContext(@event, customer, line, reservation.TotalQuantity, now);
            pricedLines.Add(new PricedLine(line, ApplyRules(context)));
        }

        return new PricedOrder(reservation.EventId, pricedLines);
    }

    private List<AppliedDiscount> ApplyRules(PricingContext context)
    {
        var applied = new List<AppliedDiscount>();
        var granted = Percentage.Zero;

        foreach (var rule in _rules)
        {
            var headroom = granted.RemainingUpTo(_maximumDiscount);
            if (headroom.IsZero)
            {
                break;
            }

            if (!rule.AppliesTo(context))
            {
                continue;
            }

            var requested = rule.Discount(context);
            if (requested.IsZero)
            {
                continue;
            }

            var effective = requested > headroom ? headroom : requested;
            granted = granted.Add(effective);
            applied.Add(new AppliedDiscount(rule.Name, effective, context.Line.Subtotal.Portion(effective)));
        }

        return applied;
    }
}
