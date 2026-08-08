using EventBooking.Domain.Common;
using EventBooking.Domain.Enums;
using EventBooking.Domain.Exceptions;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Entities;

/// <summary>
/// A named category of ticket for one event - "VIP", "Student", "Early bird stand" - with its own
/// list price, its own inventory and, optionally, its own sales window.
/// </summary>
public sealed class TicketType : Entity<TicketTypeId>
{
    public TicketType(
        TicketTypeId id,
        string name,
        TicketTier tier,
        Money basePrice,
        int capacity,
        DateRange? salesWindow = null)
        : base(id)
    {
        if (basePrice.Amount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(basePrice), basePrice, "A ticket price cannot be negative.");
        }

        Name = Guard.MaxLength(Guard.NotEmpty(name), 60);
        Tier = tier;
        BasePrice = basePrice;
        SalesWindow = salesWindow;
        Allocation = new TicketAllocation(capacity);
    }

    public string Name { get; }

    public TicketTier Tier { get; }

    public Money BasePrice { get; private set; }

    /// <summary>When <see langword="null"/>, the ticket is on sale for as long as the event is published.</summary>
    public DateRange? SalesWindow { get; }

    public TicketAllocation Allocation { get; }

    public bool IsOnSale(DateTimeOffset moment) => SalesWindow is null || SalesWindow.Contains(moment);

    /// <summary>
    /// Full validation of a request without changing anything, so that a multi-line order can be
    /// checked completely before the first seat is taken.
    /// </summary>
    public void EnsureCanReserve(int quantity, DateTimeOffset moment)
    {
        Guard.Positive(quantity);

        if (!IsOnSale(moment))
        {
            throw new BusinessRuleViolationException(
                $"'{Name}' tickets are not on sale right now (sales window: {SalesWindow}).");
        }

        if (!Allocation.CanReserve(quantity))
        {
            throw new InsufficientTicketsException(Name, quantity, Allocation.Available);
        }
    }

    public void Reserve(int quantity) => Allocation.Reserve(quantity);

    public void ConfirmReserved(int quantity) => Allocation.ConfirmReserved(quantity);

    public void ReleaseReserved(int quantity) => Allocation.ReleaseReserved(quantity);

    public void ReleaseSold(int quantity) => Allocation.ReleaseSold(quantity);

    public void ChangePrice(Money newPrice)
    {
        if (newPrice.Currency != BasePrice.Currency)
        {
            throw new CurrencyMismatchException(BasePrice.Currency, newPrice.Currency);
        }

        if (newPrice.Amount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(newPrice), newPrice, "A ticket price cannot be negative.");
        }

        BasePrice = newPrice;
    }

    public override string ToString() => $"{Name} ({BasePrice})";
}
