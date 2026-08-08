using EventBooking.Domain.Interfaces;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Notifications;

/// <summary>
/// Turns "tell this customer something" into an actual message. Every handler needs this and none of
/// them should own it, so it is a collaborator rather than a base class they all inherit from.
/// </summary>
public sealed class CustomerNotifier(IUserRepository users, INotificationChannel channel)
{
    public void Notify(UserId userId, string subject, string body)
    {
        var user = users.FindById(userId);
        if (user is null)
        {
            // A deleted account is not a reason to fail the booking that triggered the message.
            return;
        }

        channel.Send(new Notification(user.Email, subject, body));
    }
}
