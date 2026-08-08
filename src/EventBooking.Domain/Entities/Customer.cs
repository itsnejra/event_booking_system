using EventBooking.Domain.Enums;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Entities;

/// <summary>
/// Someone who books tickets. Loyalty is derived from behaviour rather than set by hand: the
/// customer counts their own completed bookings and moves up when they qualify.
/// </summary>
public sealed class Customer : User
{
    public const int SilverThreshold = 5;
    public const int GoldThreshold = 15;

    public Customer(UserId id, string fullName, EmailAddress email, MembershipTier tier = MembershipTier.Standard)
        : base(id, fullName, email)
    {
        Tier = tier;
    }

    public MembershipTier Tier { get; private set; }

    public int CompletedBookings { get; private set; }

    public override UserRole Role => UserRole.Customer;

    /// <summary>Called once a booking is paid for; may promote the customer to the next tier.</summary>
    public void RegisterCompletedBooking()
    {
        CompletedBookings++;

        // Promotion only.
        var earned = TierFor(CompletedBookings);
        if (earned > Tier)
        {
            Tier = earned;
        }
    }

    private static MembershipTier TierFor(int completedBookings) => completedBookings switch
    {
        >= GoldThreshold => MembershipTier.Gold,
        >= SilverThreshold => MembershipTier.Silver,
        _ => MembershipTier.Standard,
    };
}
