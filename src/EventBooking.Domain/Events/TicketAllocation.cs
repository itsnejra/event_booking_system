using EventBooking.Domain.Common;

namespace EventBooking.Domain.Events;

/// <summary>
/// The inventory of a single ticket type, split into three buckets that must always add up to the
/// capacity. Held seats are tracked separately from sold ones so that an abandoned checkout returns
/// its seats to the pool instead of quietly leaking them.
/// </summary>
public sealed class TicketAllocation
{
    public TicketAllocation(int capacity) => Capacity = Guard.Positive(capacity);

    public int Capacity { get; private set; }

    /// <summary>Held for a booking that has not been paid for yet.</summary>
    public int Reserved { get; private set; }

    public int Sold { get; private set; }

    public int Available => Capacity - Reserved - Sold;

    public bool IsSoldOut => Available == 0;

    public bool CanReserve(int quantity) => quantity > 0 && quantity <= Available;

    public void Reserve(int quantity)
    {
        Guard.Positive(quantity);
        EnsureEnough(quantity, Available, "reserve");
        Reserved += quantity;
    }

    public void ConfirmReserved(int quantity)
    {
        Guard.Positive(quantity);
        EnsureEnough(quantity, Reserved, "confirm");
        Reserved -= quantity;
        Sold += quantity;
    }

    public void ReleaseReserved(int quantity)
    {
        Guard.Positive(quantity);
        EnsureEnough(quantity, Reserved, "release");
        Reserved -= quantity;
    }

    public void ReleaseSold(int quantity)
    {
        Guard.Positive(quantity);
        EnsureEnough(quantity, Sold, "refund");
        Sold -= quantity;
    }

    /// <summary>Capacity can be changed, but never below what has already been promised to customers.</summary>
    public void Resize(int newCapacity)
    {
        Guard.Positive(newCapacity);
        var committed = Reserved + Sold;
        if (newCapacity < committed)
        {
            throw new InvalidOperationException(
                $"Capacity cannot drop to {newCapacity}: {committed} ticket(s) are already reserved or sold.");
        }

        Capacity = newCapacity;
    }

    // Reaching this point means a caller skipped the checks on TicketType/Event, i.e. a bug in our
    // own code rather than an invalid request from a user - hence InvalidOperationException.
    private static void EnsureEnough(int requested, int bucket, string operation)
    {
        if (requested > bucket)
        {
            throw new InvalidOperationException(
                $"Cannot {operation} {requested} ticket(s); only {bucket} available in that state.");
        }
    }
}
