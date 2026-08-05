using EventBooking.Domain.Tests.TestKit;
using EventBooking.Domain.Users;

namespace EventBooking.Domain.Tests.Users;

public sealed class CustomerTests
{
    [Fact]
    public void NewCustomer_StartsAtTheStandardTier()
    {
        Assert.Equal(MembershipTier.Standard, Given.Customer().Tier);
    }

    [Fact]
    public void FiveCompletedBookings_EarnSilver()
    {
        var customer = Given.Customer();

        Complete(customer, Customer.SilverThreshold);

        Assert.Equal(MembershipTier.Silver, customer.Tier);
    }

    [Fact]
    public void FifteenCompletedBookings_EarnGold()
    {
        var customer = Given.Customer();

        Complete(customer, Customer.GoldThreshold);

        Assert.Equal(MembershipTier.Gold, customer.Tier);
    }

    /// <summary>
    /// A tier granted by hand has to survive the customer's next purchase. Recomputing the tier from
    /// the booking count alone would silently demote them, which is the opposite of a loyalty scheme.
    /// </summary>
    [Fact]
    public void ATierGrantedUpFront_IsNotLostOnTheNextBooking()
    {
        var customer = Given.Customer(MembershipTier.Gold);

        customer.RegisterCompletedBooking();

        Assert.Equal(MembershipTier.Gold, customer.Tier);
    }

    [Fact]
    public void Organizer_ReportsTheOrganizerRole()
    {
        var organizer = new Organizer(
            Domain.ValueObjects.UserId.New(),
            "Amila",
            Domain.ValueObjects.EmailAddress.Create("amila@skyline.ba"),
            "Skyline Events");

        Assert.Equal(UserRole.Organizer, organizer.Role);
        Assert.Equal(UserRole.Customer, Given.Customer().Role);
    }

    private static void Complete(Customer customer, int times)
    {
        for (var index = 0; index < times; index++)
        {
            customer.RegisterCompletedBooking();
        }
    }
}
