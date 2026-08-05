using EventBooking.Domain.Events;

namespace EventBooking.Domain.Tests.Events;

public sealed class TicketAllocationTests
{
    [Fact]
    public void NewAllocation_HasEverythingAvailable()
    {
        var allocation = new TicketAllocation(100);

        Assert.Equal(100, allocation.Available);
        Assert.Equal(0, allocation.Reserved);
        Assert.Equal(0, allocation.Sold);
    }

    [Fact]
    public void Reserve_MovesSeatsOutOfAvailableButNotIntoSold()
    {
        var allocation = new TicketAllocation(10);

        allocation.Reserve(3);

        Assert.Equal(7, allocation.Available);
        Assert.Equal(3, allocation.Reserved);
        Assert.Equal(0, allocation.Sold);
    }

    [Fact]
    public void ConfirmReserved_MovesSeatsFromHeldToSold()
    {
        var allocation = new TicketAllocation(10);
        allocation.Reserve(3);

        allocation.ConfirmReserved(3);

        Assert.Equal(7, allocation.Available);
        Assert.Equal(0, allocation.Reserved);
        Assert.Equal(3, allocation.Sold);
    }

    [Fact]
    public void ReleaseReserved_PutsHeldSeatsBack()
    {
        var allocation = new TicketAllocation(10);
        allocation.Reserve(4);

        allocation.ReleaseReserved(4);

        Assert.Equal(10, allocation.Available);
    }

    [Fact]
    public void ReleaseSold_PutsSoldSeatsBack()
    {
        var allocation = new TicketAllocation(10);
        allocation.Reserve(4);
        allocation.ConfirmReserved(4);

        allocation.ReleaseSold(4);

        Assert.Equal(10, allocation.Available);
        Assert.Equal(0, allocation.Sold);
    }

    [Fact]
    public void BucketsAlwaysAddUpToCapacity()
    {
        var allocation = new TicketAllocation(20);
        allocation.Reserve(5);
        allocation.ConfirmReserved(2);
        allocation.Reserve(4);

        Assert.Equal(
            allocation.Capacity,
            allocation.Available + allocation.Reserved + allocation.Sold);
    }

    [Fact]
    public void Reserve_BeyondAvailability_Throws()
    {
        var allocation = new TicketAllocation(2);

        Assert.Throws<InvalidOperationException>(() => allocation.Reserve(3));
    }

    [Fact]
    public void ConfirmReserved_MoreThanIsHeld_Throws()
    {
        var allocation = new TicketAllocation(10);
        allocation.Reserve(1);

        Assert.Throws<InvalidOperationException>(() => allocation.ConfirmReserved(2));
    }

    [Fact]
    public void CanReserve_RejectsZeroAndNegativeQuantities()
    {
        var allocation = new TicketAllocation(10);

        Assert.False(allocation.CanReserve(0));
        Assert.False(allocation.CanReserve(-1));
        Assert.True(allocation.CanReserve(10));
    }

    [Fact]
    public void Resize_BelowWhatIsAlreadyPromised_Throws()
    {
        var allocation = new TicketAllocation(10);
        allocation.Reserve(4);
        allocation.ConfirmReserved(4);
        allocation.Reserve(2);

        Assert.Throws<InvalidOperationException>(() => allocation.Resize(5));
    }

    [Fact]
    public void Resize_AboveWhatIsPromised_IsAllowed()
    {
        var allocation = new TicketAllocation(10);
        allocation.Reserve(4);

        allocation.Resize(20);

        Assert.Equal(16, allocation.Available);
    }
}
